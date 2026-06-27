using WalletTracker.Domain;

namespace WalletTracker.Application.Services;

/// <summary>
/// FIFO cost-basis matching. All math is pure/static so it's trivially unit-testable
/// without touching the database.
/// </summary>
public static class PnLCalculator
{
    public record SellResult(decimal RealizedPnL, bool IsWin);

    /// <summary>
    /// Consumes oldest-first lots in <paramref name="lots"/> to cover a sell of <paramref name="quantitySold"/>
    /// at <paramref name="sellPricePerToken"/>. Mutates lot quantities in place (removing fully-consumed lots
    /// is the caller's responsibility based on QuantityRemaining == 0).
    /// </summary>
    public static SellResult ApplyFifoSell(List<CostBasisLot> lots, decimal quantitySold, decimal sellPricePerToken)
    {
        var remaining = quantitySold;
        decimal costBasisConsumed = 0;

        foreach (var lot in lots.OrderBy(l => l.AcquiredAt))
        {
            if (remaining <= 0) break;
            if (lot.QuantityRemaining <= 0) continue;

            var consumeQty = Math.Min(lot.QuantityRemaining, remaining);
            costBasisConsumed += consumeQty * lot.PricePerTokenInQuote;
            lot.QuantityRemaining -= consumeQty;
            remaining -= consumeQty;
        }

        var proceeds = quantitySold * sellPricePerToken;
        var realizedPnL = proceeds - costBasisConsumed;
        return new SellResult(realizedPnL, realizedPnL > 0);
    }

    public static decimal CalculateUnrealizedPnL(IEnumerable<CostBasisLot> openLots, decimal lastKnownPrice)
    {
        decimal unrealized = 0;
        foreach (var lot in openLots.Where(l => l.QuantityRemaining > 0))
        {
            unrealized += lot.QuantityRemaining * (lastKnownPrice - lot.PricePerTokenInQuote);
        }
        return unrealized;
    }
}
