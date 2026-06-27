namespace WalletTracker.Domain;

public class WalletStats
{
    public int WalletId { get; set; }
    public TrackedWallet? Wallet { get; set; }

    public int TotalTrades { get; set; }
    public int TotalSells { get; set; }
    public int WinningSells { get; set; }
    public decimal WinRate { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Realized/unrealized PnL for a wallet, broken out per quote asset since amounts in different
/// quote assets (SOL vs USDC vs WETH) are never numerically comparable without an external price feed.</summary>
public class QuotePnL
{
    public long Id { get; set; }
    public int WalletId { get; set; }
    public TrackedWallet? Wallet { get; set; }

    public string QuoteSymbol { get; set; } = string.Empty;
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
