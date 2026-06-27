using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WalletTracker.Application.Interfaces;
using WalletTracker.Domain;

namespace WalletTracker.Application.Services;

public class NotificationDispatcher
{
    private readonly IWalletTrackerDbContext _db;
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(IWalletTrackerDbContext db, IEnumerable<INotificationSender> senders, ILogger<NotificationDispatcher> logger)
    {
        _db = db;
        _senders = senders;
        _logger = logger;
    }

    public async Task DispatchAsync(string message, CancellationToken ct)
    {
        var enabledChannels = await _db.NotificationChannels.Where(c => c.IsEnabled).ToListAsync(ct);

        foreach (var channel in enabledChannels)
        {
            var sender = _senders.FirstOrDefault(s => s.Type == channel.Type);
            if (sender is null) continue;

            try
            {
                await sender.SendAsync(channel, message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending {ChannelType} notification", channel.Type);
            }
        }
    }

    public static string FormatTradeMessage(TrackedWallet wallet, Trade trade) =>
        $"{(trade.Direction == TradeDirection.Buy ? "🟢 BUY" : "🔴 SELL")} | {wallet.Label ?? wallet.Address} ({trade.Chain}) | " +
        $"{trade.AmountToken:N4} {trade.TokenSymbol ?? trade.TokenAddress} @ {trade.PricePerTokenInQuote:N8} | tx: {trade.TxHash}";

    public static string FormatNewTokenMessage(TrackedWallet wallet, NewTokenAlert alert) =>
        $"🆕 New token for {wallet.Label ?? wallet.Address} ({alert.Wallet?.Chain}): {alert.TokenSymbol ?? alert.TokenAddress}";
}
