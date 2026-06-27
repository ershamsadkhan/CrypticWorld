using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using WalletTracker.Application.Interfaces;
using WalletTracker.Application.Services;
using WalletTracker.Domain;
using WalletTracker.Infrastructure.Realtime;

namespace WalletTracker.Infrastructure.Chains.Evm;

/// <summary>
/// Polling EVM indexer shared by Ethereum/BSC/Base. Decodes swaps by diffing ERC20 Transfer
/// events that touch the wallet within a transaction: the wrapped-native-token (WETH/WBNB) leg
/// is treated as the quote amount, and the other token leg as the traded token. This generalizes
/// across Uniswap V2/V3, PancakeSwap, BaseSwap etc. without hardcoding each router's ABI.
/// </summary>
public class EvmIndexer : IChainIndexer
{
    private const string TransferEventSignature = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

    private readonly Web3 _web3;
    private readonly string _wrappedNativeAddress;
    private readonly string _wrappedNativeSymbol;
    private readonly IWalletTrackerDbContext _db;
    private readonly TradePersistenceService _persistence;
    private readonly NotificationDispatcher _dispatcher;
    private readonly WalletEventBroadcaster _broadcaster;
    private readonly ILogger<EvmIndexer> _logger;

    public Chain Chain { get; }

    public EvmIndexer(
        Chain chain,
        IOptions<ChainRpcOptions> rpcOptions,
        IWalletTrackerDbContext db,
        TradePersistenceService persistence,
        NotificationDispatcher dispatcher,
        WalletEventBroadcaster broadcaster,
        ILogger<EvmIndexer> logger)
    {
        Chain = chain;
        var options = chain switch
        {
            Chain.Ethereum => rpcOptions.Value.Ethereum,
            Chain.Bsc => rpcOptions.Value.Bsc,
            Chain.Base => rpcOptions.Value.Base,
            _ => throw new ArgumentOutOfRangeException(nameof(chain), chain, "Not an EVM chain")
        };

        var url = options.Urls.FirstOrDefault() ?? throw new InvalidOperationException($"No RPC URL configured for {chain}");
        _web3 = new Web3(url);
        _wrappedNativeAddress = KnownAddresses.WrappedNative[chain].ToLowerInvariant();
        _wrappedNativeSymbol = chain == Chain.Bsc ? "WBNB" : "WETH";
        _db = db;
        _persistence = persistence;
        _dispatcher = dispatcher;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    /// <summary>Max block span per eth_getLogs call. Public RPC endpoints commonly cap this around 2k-10k blocks.</summary>
    private const long BackfillChunkSize = 2000;

    public async Task PollNewActivityAsync(TrackedWallet wallet, CancellationToken ct)
    {
        var latestBlock = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        var latestBlockNumber = (long)latestBlock.Value;

        var fromBlock = wallet.LastCursor is not null && long.TryParse(wallet.LastCursor, out var lastBlock)
            ? lastBlock + 1
            : Math.Max(0, latestBlockNumber - 2000); // first poll: look back a bounded window

        if (fromBlock > latestBlockNumber) return;

        await ScanRangeAsync(wallet, fromBlock, latestBlockNumber, ct);

        wallet.LastCursor = latestBlockNumber.ToString();
        await _db.SaveChangesAsync(ct);
    }

    public async Task BackfillAsync(TrackedWallet wallet, CancellationToken ct)
    {
        wallet.BackfillStatus = BackfillStatus.InProgress;
        await _db.SaveChangesAsync(ct);

        try
        {
            var latestBlock = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var latestBlockNumber = (long)latestBlock.Value;

            for (var chunkStart = 0L; chunkStart <= latestBlockNumber; chunkStart += BackfillChunkSize)
            {
                ct.ThrowIfCancellationRequested();
                var chunkEnd = Math.Min(chunkStart + BackfillChunkSize - 1, latestBlockNumber);

                try
                {
                    await ScanRangeAsync(wallet, chunkStart, chunkEnd, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Backfill chunk {From}-{To} failed for {Address} ({Chain}), continuing",
                        chunkStart, chunkEnd, wallet.Address, Chain);
                }

                // Persist progress incrementally so a later failure doesn't lose already-scanned ground.
                wallet.LastCursor = chunkEnd.ToString();
                await _db.SaveChangesAsync(ct);
            }

            wallet.BackfillStatus = BackfillStatus.Completed;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill failed for wallet {Address} ({Chain})", wallet.Address, Chain);
            wallet.BackfillStatus = BackfillStatus.Failed;
            await _db.SaveChangesAsync(ct);
            throw;
        }
    }

    private async Task ScanRangeAsync(TrackedWallet wallet, long fromBlock, long toBlock, CancellationToken ct)
    {
        var addressTopic = "0x000000000000000000000000" + wallet.Address[2..].ToLowerInvariant();

        var fromBlockParam = new BlockParameter(new HexBigInteger(fromBlock));
        var toBlockParam = new BlockParameter(new HexBigInteger(toBlock));

        var outgoingFilter = new NewFilterInput
        {
            FromBlock = fromBlockParam,
            ToBlock = toBlockParam,
            Topics = new object[] { TransferEventSignature, addressTopic }
        };
        var incomingFilter = new NewFilterInput
        {
            FromBlock = fromBlockParam,
            ToBlock = toBlockParam,
            Topics = new object[] { TransferEventSignature, null!, addressTopic }
        };

        var outgoingLogs = await _web3.Eth.Filters.GetLogs.SendRequestAsync(outgoingFilter);
        var incomingLogs = await _web3.Eth.Filters.GetLogs.SendRequestAsync(incomingFilter);

        var byTxHash = outgoingLogs.Concat(incomingLogs)
            .GroupBy(l => l.TransactionHash)
            .OrderBy(g => g.First().BlockNumber.Value);

        foreach (var txLogs in byTxHash)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessTransactionAsync(wallet, txLogs.Key, txLogs.ToList(), ct);
        }
    }

    private async Task ProcessTransactionAsync(TrackedWallet wallet, string txHash, List<FilterLog> logs, CancellationToken ct)
    {
        var walletLower = wallet.Address.ToLowerInvariant();

        // Net token delta for the wallet, grouped by ERC20 contract address.
        var deltas = new Dictionary<string, decimal>();
        foreach (var log in logs)
        {
            if (log.Topics.Length < 3) continue;
            var from = "0x" + log.Topics[1].ToString()[26..];
            var to = "0x" + log.Topics[2].ToString()[26..];
            var amount = new HexBigInteger(log.Data).Value;
            var contract = log.Address.ToLowerInvariant();

            decimal signedAmount = (decimal)amount; // raw integer units; decimals normalization is best-effort (see note below)
            if (to.Equals(walletLower, StringComparison.OrdinalIgnoreCase))
            {
                deltas[contract] = deltas.GetValueOrDefault(contract) + signedAmount;
            }
            if (from.Equals(walletLower, StringComparison.OrdinalIgnoreCase))
            {
                deltas[contract] = deltas.GetValueOrDefault(contract) - signedAmount;
            }
        }

        deltas = deltas.Where(d => d.Value != 0).ToDictionary(d => d.Key, d => d.Value);
        if (!deltas.TryGetValue(_wrappedNativeAddress, out var quoteDeltaRaw)) return;

        var tokenLeg = deltas.FirstOrDefault(d => d.Key != _wrappedNativeAddress);
        if (tokenLeg.Key is null) return;

        // Normalize using on-chain decimals() for both legs so price math is correct.
        var quoteDecimals = await GetDecimalsAsync(_wrappedNativeAddress);
        var tokenDecimals = await GetDecimalsAsync(tokenLeg.Key);

        var quoteDelta = quoteDeltaRaw / (decimal)Math.Pow(10, quoteDecimals);
        var tokenDelta = tokenLeg.Value / (decimal)Math.Pow(10, tokenDecimals);

        // Wallet spent wrapped-native (negative) and received token (positive) => Buy, and vice versa for Sell.
        var direction = tokenDelta > 0 ? TradeDirection.Buy : TradeDirection.Sell;
        var amountToken = Math.Abs(tokenDelta);
        var amountQuote = Math.Abs(quoteDelta);
        if (amountToken == 0 || amountQuote == 0) return;

        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(logs.First().BlockNumber.Value));

        var trade = new Trade
        {
            WalletId = wallet.Id,
            Chain = Chain,
            TxHash = txHash,
            BlockTime = DateTimeOffset.FromUnixTimeSeconds((long)block.Timestamp.Value).UtcDateTime,
            Direction = direction,
            TokenAddress = tokenLeg.Key,
            AmountToken = amountToken,
            AmountQuote = amountQuote,
            PricePerTokenInQuote = amountQuote / amountToken,
            QuoteSymbol = _wrappedNativeSymbol,
            DexName = $"{Chain} DEX (aggregated)"
        };

        var result = await _persistence.PersistTradeAsync(wallet, trade, ct);
        if (result is null) return;

        await _broadcaster.BroadcastTradeAsync(trade, ct);
        await _dispatcher.DispatchAsync(NotificationDispatcher.FormatTradeMessage(wallet, trade), ct);
        if (result.IsNewToken)
        {
            await _broadcaster.BroadcastNewTokenAsync(wallet.Id, trade.TokenAddress, trade.TokenSymbol, ct);
            await _dispatcher.DispatchAsync($"🆕 New token for {wallet.Label ?? wallet.Address} ({Chain}): {trade.TokenSymbol ?? trade.TokenAddress}", ct);
        }
    }

    private readonly Dictionary<string, int> _decimalsCache = new();

    private async Task<int> GetDecimalsAsync(string contractAddress)
    {
        if (_decimalsCache.TryGetValue(contractAddress, out var cached)) return cached;

        try
        {
            var contract = _web3.Eth.GetContract(Erc20Abi.MinimalAbi, contractAddress);
            var decimalsFunction = contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            _decimalsCache[contractAddress] = decimals;
            return decimals;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read decimals() for {Contract}, defaulting to 18", contractAddress);
            _decimalsCache[contractAddress] = 18;
            return 18;
        }
    }
}
