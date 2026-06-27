using Microsoft.AspNetCore.SignalR;
using WalletTracker.Domain;

namespace WalletTracker.Infrastructure.Realtime;

public class WalletEventBroadcaster
{
    private readonly IHubContext<WalletHub> _hubContext;

    public WalletEventBroadcaster(IHubContext<WalletHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastTradeAsync(Trade trade, CancellationToken ct) =>
        _hubContext.Clients.All.SendAsync("trade", new
        {
            trade.WalletId,
            trade.Chain,
            trade.TxHash,
            trade.BlockTime,
            trade.Direction,
            trade.TokenAddress,
            trade.TokenSymbol,
            trade.AmountToken,
            trade.AmountQuote,
            trade.PricePerTokenInQuote
        }, ct);

    public Task BroadcastNewTokenAsync(int walletId, string tokenAddress, string? tokenSymbol, CancellationToken ct) =>
        _hubContext.Clients.All.SendAsync("newToken", new { walletId, tokenAddress, tokenSymbol }, ct);
}
