using System.Net.Http.Json;
using System.Text.Json;
using WalletTracker.Application.Interfaces;
using WalletTracker.Application.Notifications;
using WalletTracker.Domain;

namespace WalletTracker.Infrastructure.Notifications;

public class TelegramSender : INotificationSender
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationChannelType Type => NotificationChannelType.Telegram;

    public TelegramSender(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(NotificationChannel channel, string message, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<TelegramConfig>(channel.ConfigJson, JsonOptions.CaseInsensitive)
            ?? throw new InvalidOperationException("Invalid Telegram channel config");

        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.telegram.org/bot{config.BotToken}/sendMessage";
        var response = await client.PostAsJsonAsync(url, new { chat_id = config.ChatId, text = message }, ct);
        response.EnsureSuccessStatusCode();
    }
}
