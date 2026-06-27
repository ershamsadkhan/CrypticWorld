using System.Text;
using System.Text.Json;
using WalletTracker.Application.Interfaces;
using WalletTracker.Application.Notifications;
using WalletTracker.Domain;

namespace WalletTracker.Infrastructure.Notifications;

/// <summary>Sends WhatsApp messages via Twilio's WhatsApp API.</summary>
public class WhatsAppSender : INotificationSender
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationChannelType Type => NotificationChannelType.WhatsApp;

    public WhatsAppSender(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(NotificationChannel channel, string message, CancellationToken ct)
    {
        var config = JsonSerializer.Deserialize<WhatsAppConfig>(channel.ConfigJson, JsonOptions.CaseInsensitive)
            ?? throw new InvalidOperationException("Invalid WhatsApp channel config");

        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{config.AccountSid}/Messages.json";

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.AccountSid}:{config.AuthToken}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Basic {basicAuth}");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["From"] = $"whatsapp:{config.FromNumber}",
            ["To"] = $"whatsapp:{config.ToNumber}",
            ["Body"] = message
        });

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
