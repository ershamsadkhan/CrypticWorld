using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Solnet.Rpc;
using Solnet.Rpc.Models;
using WalletTracker.Application.Interfaces;
using WalletTracker.Application.Services;
using WalletTracker.Domain;
using WalletTracker.Infrastructure.Realtime;

namespace WalletTracker.Infrastructure.Chains.Solana;

/// <summary>
/// Polls Solana for new signatures involving a tracked wallet, decodes swaps by diffing
/// pre/post native SOL and SPL token balances (works generically across Jupiter/Raydium/Orca
/// without needing per-DEX instruction parsing).
/// </summary>
public class SolanaIndexer : IChainIndexer
{
    private const string UsdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
    private const string UsdtMint = "Es9vMFrzaCERmJfrF4H2FYD4KCoNkY11McCe8BenwNYB";

    /// <summary>Below this, a native SOL balance change is assumed to be just the transaction fee/tip,
    /// not a real swap leg. Real SOL-denominated swaps are virtually always larger than this.</summary>
    private const decimal SolFeeNoiseThreshold = 0.0003m;

    private readonly IRpcClient _rpcClient;
    private readonly IWalletTrackerDbContext _db;
    private readonly TradePersistenceService _persistence;
    private readonly NotificationDispatcher _dispatcher;
    private readonly WalletEventBroadcaster _broadcaster;
    private readonly ILogger<SolanaIndexer> _logger;

    public Chain Chain => Chain.Solana;

    public SolanaIndexer(
        IOptions<ChainRpcOptions> rpcOptions,
        IWalletTrackerDbContext db,
        TradePersistenceService persistence,
        NotificationDispatcher dispatcher,
        WalletEventBroadcaster broadcaster,
        ILogger<SolanaIndexer> logger)
    {
        var url = rpcOptions.Value.Solana.Urls.FirstOrDefault() ?? "https://api.mainnet-beta.solana.com";
        _rpcClient = ClientFactory.GetClient(url);
        _db = db;
        _persistence = persistence;
        _dispatcher = dispatcher;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task PollNewActivityAsync(TrackedWallet wallet, CancellationToken ct)
    {
        var sigsResult = await _rpcClient.GetSignaturesForAddressAsync(wallet.Address, limit: 25, until: wallet.LastCursor);
        if (!sigsResult.WasSuccessful || sigsResult.Result is null || sigsResult.Result.Count == 0)
        {
            return;
        }

        // RPC returns newest-first; process oldest-first so cursor advances correctly.
        var newSignatures = sigsResult.Result.AsEnumerable().Reverse().ToList();

        foreach (var sigInfo in newSignatures)
        {
            ct.ThrowIfCancellationRequested();
            if (sigInfo.Error != null) continue;

            await ProcessTransactionAsync(wallet, sigInfo.Signature, ct);
        }

        wallet.LastCursor = sigsResult.Result.First().Signature; // newest processed
        await _db.SaveChangesAsync(ct);
    }

    public async Task BackfillAsync(TrackedWallet wallet, CancellationToken ct)
    {
        wallet.BackfillStatus = BackfillStatus.InProgress;
        await _db.SaveChangesAsync(ct);

        try
        {
            string? newestSignature = null;
            string? beforeCursor = null;
            var allSignatures = new List<string>();

            // Page backwards via `before` until a short page (or empty page) signals the end of history.
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var page = await _rpcClient.GetSignaturesForAddressAsync(wallet.Address, limit: 1000, before: beforeCursor);
                if (!page.WasSuccessful || page.Result is null || page.Result.Count == 0)
                {
                    break;
                }

                newestSignature ??= page.Result.First().Signature;
                allSignatures.AddRange(page.Result.Where(s => s.Error is null).Select(s => s.Signature));

                if (page.Result.Count < 1000) break;
                beforeCursor = page.Result.Last().Signature;
            }

            // Process oldest-first so PnL/cost-basis lots build up in chronological order.
            allSignatures.Reverse();
            foreach (var signature in allSignatures)
            {
                ct.ThrowIfCancellationRequested();
                await ProcessTransactionAsync(wallet, signature, ct);
            }

            wallet.LastCursor = newestSignature ?? wallet.LastCursor;
            wallet.BackfillStatus = BackfillStatus.Completed;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill failed for wallet {Address}", wallet.Address);
            wallet.BackfillStatus = BackfillStatus.Failed;
            await _db.SaveChangesAsync(ct);
            throw;
        }
    }

    private async Task ProcessTransactionAsync(TrackedWallet wallet, string signature, CancellationToken ct)
    {
        var txResult = await _rpcClient.GetTransactionAsync(signature, Solnet.Rpc.Types.Commitment.Confirmed);
        if (!txResult.WasSuccessful || txResult.Result?.Meta is null)
        {
            return;
        }

        var meta = txResult.Result.Meta;
        var accountKeys = txResult.Result.Transaction.Message.AccountKeys;

        var walletIndex = Array.IndexOf(accountKeys, wallet.Address);
        if (walletIndex < 0) return;

        // Native SOL delta for the wallet account (lamports -> SOL).
        decimal nativeDelta = 0;
        if (meta.PreBalances != null && meta.PostBalances != null
            && walletIndex < meta.PreBalances.Length && walletIndex < meta.PostBalances.Length)
        {
            nativeDelta = (meta.PostBalances[walletIndex] - meta.PreBalances[walletIndex]) / 1_000_000_000m;
        }

        // Solnet's TokenBalanceInfo has no Owner field, so we diff every token balance entry in the
        // transaction by mint. This is correct for the common case of a wallet-initiated swap where
        // only the wallet's own associated token accounts change balance.
        var preTokenBalances = (meta.PreTokenBalances ?? Array.Empty<TokenBalanceInfo>())
            .GroupBy(b => b.Mint).ToDictionary(g => g.Key, g => g.Sum(b => ParseUiAmount(b.UiTokenAmount?.UiAmountString)));
        var postTokenBalances = (meta.PostTokenBalances ?? Array.Empty<TokenBalanceInfo>())
            .GroupBy(b => b.Mint).ToDictionary(g => g.Key, g => g.Sum(b => ParseUiAmount(b.UiTokenAmount?.UiAmountString)));

        var allMints = preTokenBalances.Keys.Union(postTokenBalances.Keys).ToList();
        var tokenDeltas = allMints
            .Select(mint => (Mint: mint, Delta: postTokenBalances.GetValueOrDefault(mint) - preTokenBalances.GetValueOrDefault(mint)))
            .Where(x => Math.Abs(x.Delta) > 0.000000001m)
            .ToList();

        string tokenAddress;
        TradeDirection direction;
        decimal amountToken;
        decimal amountQuote;
        string quoteSymbol;

        if (tokenDeltas.Count == 2)
        {
            // Two SPL legs moved: either a stable<->token swap or an arbitrary token<->token swap.
            // We only record it if one leg is a recognized quote asset (USDC/USDT) — otherwise there's
            // no canonical numeraire to track PnL against, so it's skipped rather than guessing one.
            var quoteLeg = tokenDeltas.FirstOrDefault(x => x.Mint == UsdcMint || x.Mint == UsdtMint);
            if (quoteLeg.Mint is null) return;

            var tokenLeg = tokenDeltas.First(x => x.Mint != quoteLeg.Mint);
            tokenAddress = tokenLeg.Mint;
            direction = tokenLeg.Delta > 0 ? TradeDirection.Buy : TradeDirection.Sell;
            amountToken = Math.Abs(tokenLeg.Delta);
            amountQuote = Math.Abs(quoteLeg.Delta);
            quoteSymbol = quoteLeg.Mint == UsdcMint ? "USDC" : "USDT";
        }
        else if (tokenDeltas.Count == 1 && Math.Abs(nativeDelta) >= SolFeeNoiseThreshold)
        {
            // One SPL leg moved against the wallet's native SOL balance — the classic SOL<->token swap.
            tokenAddress = tokenDeltas[0].Mint;
            direction = tokenDeltas[0].Delta > 0 ? TradeDirection.Buy : TradeDirection.Sell;
            amountToken = Math.Abs(tokenDeltas[0].Delta);
            amountQuote = Math.Abs(nativeDelta);
            quoteSymbol = "SOL";
        }
        else
        {
            // Ambiguous, multi-hop, or dust-only — skip rather than guess.
            return;
        }

        if (amountToken == 0 || amountQuote == 0) return;

        var blockTime = txResult.Result.BlockTime.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(txResult.Result.BlockTime.Value).UtcDateTime
            : DateTime.UtcNow;

        var trade = new Trade
        {
            WalletId = wallet.Id,
            Chain = Chain.Solana,
            TxHash = signature,
            BlockTime = blockTime,
            Direction = direction,
            TokenAddress = tokenAddress,
            AmountToken = amountToken,
            AmountQuote = amountQuote,
            PricePerTokenInQuote = amountQuote / amountToken,
            QuoteSymbol = quoteSymbol,
            DexName = "Solana DEX (aggregated)"
        };

        var result = await _persistence.PersistTradeAsync(wallet, trade, ct);
        if (result is null) return;

        await _broadcaster.BroadcastTradeAsync(trade, ct);
        await _dispatcher.DispatchAsync(NotificationDispatcher.FormatTradeMessage(wallet, trade), ct);
        if (result.IsNewToken)
        {
            await _broadcaster.BroadcastNewTokenAsync(wallet.Id, trade.TokenAddress, trade.TokenSymbol, ct);
            await _dispatcher.DispatchAsync($"🆕 New token for {wallet.Label ?? wallet.Address} (Solana): {trade.TokenSymbol ?? trade.TokenAddress}", ct);
        }
    }

    private static decimal ParseUiAmount(string? uiAmountString) =>
        decimal.TryParse(uiAmountString, out var value) ? value : 0m;
}
