namespace WalletTracker.Domain;

public class NotificationChannel
{
    public int Id { get; set; }
    public NotificationChannelType Type { get; set; }

    /// <summary>JSON blob holding channel-specific config: bot token/chat id, webhook URL, WhatsApp API creds.</summary>
    public string ConfigJson { get; set; } = "{}";
    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
