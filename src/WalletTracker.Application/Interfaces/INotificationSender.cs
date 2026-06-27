using WalletTracker.Domain;

namespace WalletTracker.Application.Interfaces;

public interface INotificationSender
{
    NotificationChannelType Type { get; }
    Task SendAsync(NotificationChannel channel, string message, CancellationToken ct);
}
