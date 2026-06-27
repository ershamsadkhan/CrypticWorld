namespace WalletTracker.Domain;

public class TrackedWallet
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public Chain Chain { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    /// <summary>Cursor for incremental polling: last signature (Solana) or block number (EVM) already processed.</summary>
    public string? LastCursor { get; set; }

    public BackfillStatus BackfillStatus { get; set; } = BackfillStatus.NotStarted;

    public List<Trade> Trades { get; set; } = new();
    public List<TokenPosition> Positions { get; set; } = new();
    public WalletStats? Stats { get; set; }
}
