using System.Text.Json;

namespace WalletTracker.Infrastructure.Notifications;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };
}
