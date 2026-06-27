using Microsoft.AspNetCore.SignalR;

namespace WalletTracker.Infrastructure.Realtime;

/// <summary>Push channel for live trade/alert updates. Clients just connect and listen; no server-invokable methods needed for v1.</summary>
public class WalletHub : Hub
{
}
