namespace WalletTracker.Domain;

public class Trade
{
    public long Id { get; set; }
    public int WalletId { get; set; }
    public TrackedWallet? Wallet { get; set; }

    public Chain Chain { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public DateTime BlockTime { get; set; }
    public TradeDirection Direction { get; set; }

    public string TokenAddress { get; set; } = string.Empty;
    public string? TokenSymbol { get; set; }

    public decimal AmountToken { get; set; }
    /// <summary>Amount of the quote asset involved in the swap, denominated in <see cref="QuoteSymbol"/>.</summary>
    public decimal AmountQuote { get; set; }
    public decimal PricePerTokenInQuote { get; set; }
    /// <summary>The quote asset this trade is denominated in (e.g. "SOL", "USDC", "WETH"). Trades against
    /// different quote assets are never summed together numerically — PnL is reported per quote symbol.</summary>
    public string QuoteSymbol { get; set; } = string.Empty;

    public string? DexName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
