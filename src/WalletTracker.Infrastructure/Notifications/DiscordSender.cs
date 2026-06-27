using System.Net.Http.Json;
using System.Text.Json;
using WalletTracker.Application.Interfaces;
using WalletTracker.Application.Notifications;
using WalletTracker.Domain;

namespace WalletTracker.Infrastructure.Notifications;

public class DiscordSender : INotificationSender
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationChannelType Type => NotificationChannelType.Discord;

    public DiscordSender(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(NotificationChannel channel, string message, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<DiscordConfig>(channel.ConfigJson, JsonOptions.CaseInsensitive)
            ?? throw new InvalidOperationException("Invalid Discord channel config");

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(config.WebhookUrl, new { content = message }, ct);
        response.EnsureSuccessStatusCode();
    }
}
