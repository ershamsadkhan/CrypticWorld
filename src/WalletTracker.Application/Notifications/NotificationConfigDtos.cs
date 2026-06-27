namespace WalletTracker.Application.Notifications;

public record TelegramConfig(string BotToken, string ChatId);
public record DiscordConfig(string WebhookUrl);
public record WhatsAppConfig(string AccountSid, string AuthToken, string FromNumber, string ToNumber);
