using WalletTracker.Domain;

namespace WalletTracker.Application.Dtos;

public record UpsertNotificationChannelRequest(NotificationChannelType Type, string ConfigJson, bool IsEnabled);

public record NotificationChannelDto(int Id, NotificationChannelType Type, string ConfigJson, bool IsEnabled, DateTime UpdatedAt);
