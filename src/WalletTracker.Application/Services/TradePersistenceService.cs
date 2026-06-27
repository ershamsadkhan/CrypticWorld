using Microsoft.EntityFrameworkCore;
using WalletTracker.Application.Interfaces;
using WalletTracker.Domain;

namespace WalletTracker.Application.Services;

/// <summary>
/// Single entry point indexers call after decoding a swap: persists the Trade, updates the
/// FIFO cost-basis lots / TokenPosition (scoped per quote asset, since a token bought with SOL
/// and a token bought with USDC are tracked as separate positions to avoid mixing denominations),
/// recomputes WalletStats and per-quote PnL incrementally, and raises a NewTokenAlert the first
/// time a wallet trades a given token (regardless of which quote asset was used). Notification
/// dispatch is the caller's responsibility (it happens after this returns).
/// </summary>
public class TradePersistenceService
{
    private readonly IWalletTrackerDbContext _db;

    public TradePersistenceService(IWalletTrackerDbContext db)
    {
        _db = db;
    }

    public record PersistResult(Trade Trade, bool IsNewToken, decimal? RealizedPnLForThisTrade);

    public async Task<PersistResult?> PersistTradeAsync(TrackedWallet wallet, Trade trade, CancellationToken ct)
    {
        var alreadyExists = await _db.Trades.AnyAsync(t => t.WalletId == wallet.Id && t.TxHash == trade.TxHash, ct);
        if (alreadyExists) return null;

        // Checked before adding the trade to context so it doesn't see its own pending insert.
        var isNewToken = !await _db.Trades.AnyAsync(t => t.WalletId == wallet.Id && t.TokenAddress == trade.TokenAddress, ct);

        _db.Trades.Add(trade);

        var position = await _db.TokenPositions
            .Include(p => p.Lots)
            .FirstOrDefaultAsync(p => p.WalletId == wallet.Id && p.TokenAddress == trade.TokenAddress && p.QuoteSymbol == trade.QuoteSymbol, ct);

        if (position is null)
        {
            position = new TokenPosition
            {
                WalletId = wallet.Id,
                TokenAddress = trade.TokenAddress,
                TokenSymbol = trade.TokenSymbol,
                QuoteSymbol = trade.QuoteSymbol,
                QuantityHeld = 0,
                LastKnownPriceInQuote = trade.PricePerTokenInQuote
            };
            _db.TokenPositions.Add(position);
        }

        if (isNewToken)
        {
            _db.NewTokenAlerts.Add(new NewTokenAlert
            {
                WalletId = wallet.Id,
                TokenAddress = trade.TokenAddress,
                TokenSymbol = trade.TokenSymbol,
                FirstSeenAt = trade.BlockTime
            });
        }

        decimal? realizedPnL = null;

        if (trade.Direction == TradeDirection.Buy)
        {
            position.Lots.Add(new CostBasisLot
            {
                QuantityRemaining = trade.AmountToken,
                PricePerTokenInQuote = trade.PricePerTokenInQuote,
                AcquiredAt = trade.BlockTime,
                SourceTradeId = trade.Id
            });
            position.QuantityHeld += trade.AmountToken;
        }
        else
        {
            var sellResult = PnLCalculator.ApplyFifoSell(position.Lots, trade.AmountToken, trade.PricePerTokenInQuote);
            realizedPnL = sellResult.RealizedPnL;
            position.QuantityHeld -= trade.AmountToken;
        }

        position.LastKnownPriceInQuote = trade.PricePerTokenInQuote;

        await UpdateWalletStatsAsync(wallet.Id, trade.Direction, trade.QuoteSymbol, realizedPnL, ct);

        await _db.SaveChangesAsync(ct);

        return new PersistResult(trade, isNewToken, realizedPnL);
    }

    private async Task UpdateWalletStatsAsync(int walletId, TradeDirection direction, string quoteSymbol, decimal? realizedPnL, CancellationToken ct)
    {
        var stats = await _db.WalletStats.FirstOrDefaultAsync(s => s.WalletId == walletId, ct);
        if (stats is null)
        {
            stats = new WalletStats { WalletId = walletId };
            _db.WalletStats.Add(stats);
        }

        stats.TotalTrades += 1;

        if (direction == TradeDirection.Sell && realizedPnL.HasValue)
        {
            stats.TotalSells += 1;
            if (realizedPnL.Value > 0) stats.WinningSells += 1;
            stats.WinRate = stats.TotalSells > 0 ? (decimal)stats.WinningSells / stats.TotalSells * 100m : 0;
        }

        stats.UpdatedAt = DateTime.UtcNow;

        var quotePnL = await _db.QuotePnLs.FirstOrDefaultAsync(q => q.WalletId == walletId && q.QuoteSymbol == quoteSymbol, ct);
        if (quotePnL is null)
        {
            quotePnL = new QuotePnL { WalletId = walletId, QuoteSymbol = quoteSymbol };
            _db.QuotePnLs.Add(quotePnL);
        }

        if (direction == TradeDirection.Sell && realizedPnL.HasValue)
        {
            quotePnL.RealizedPnL += realizedPnL.Value;
        }

        var openLots = await _db.CostBasisLots
            .Join(_db.TokenPositions, l => l.TokenPositionId, p => p.Id, (l, p) => new { l, p })
            .Where(x => x.p.WalletId == walletId && x.p.QuoteSymbol == quoteSymbol && x.l.QuantityRemaining > 0)
            .Select(x => new { x.l.QuantityRemaining, x.l.PricePerTokenInQuote, x.p.LastKnownPriceInQuote })
            .ToListAsync(ct);

        quotePnL.UnrealizedPnL = openLots.Sum(l => l.QuantityRemaining * (l.LastKnownPriceInQuote - l.PricePerTokenInQuote));
        quotePnL.UpdatedAt = DateTime.UtcNow;
    }
}
