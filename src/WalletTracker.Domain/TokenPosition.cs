namespace WalletTracker.Domain;

public class TokenPosition
{
    public long Id { get; set; }
    public int WalletId { get; set; }
    public TrackedWallet? Wallet { get; set; }

    public string TokenAddress { get; set; } = string.Empty;
    public string? TokenSymbol { get; set; }
    /// <summary>A token held via different quote assets (e.g. bought with both SOL and USDC) is tracked
    /// as separate positions, one per quote asset, so cost-basis math never mixes denominations.</summary>
    public string QuoteSymbol { get; set; } = string.Empty;
    public decimal QuantityHeld { get; set; }
    public decimal LastKnownPriceInQuote { get; set; }

    public List<CostBasisLot> Lots { get; set; } = new();
}

/// <summary>An open (unconsumed) or partially-consumed FIFO buy lot used for cost-basis matching.</summary>
public class CostBasisLot
{
    public long Id { get; set; }
    public long TokenPositionId { get; set; }
    public TokenPosition? TokenPosition { get; set; }

    public decimal QuantityRemaining { get; set; }
    public decimal PricePerTokenInQuote { get; set; }
    public DateTime AcquiredAt { get; set; }
    public long SourceTradeId { get; set; }
}
