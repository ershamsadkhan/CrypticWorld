namespace WalletTracker.Domain;

public class NewTokenAlert
{
    public long Id { get; set; }
    public int WalletId { get; set; }
    public TrackedWallet? Wallet { get; set; }

    public string TokenAddress { get; set; } = string.Empty;
    public string? TokenSymbol { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public bool Notified { get; set; }
}
