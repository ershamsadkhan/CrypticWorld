using Microsoft.EntityFrameworkCore;
using WalletTracker.Domain;

namespace WalletTracker.Application.Interfaces;

/// <summary>Abstraction over the EF Core DbContext so Application-layer services don't depend on Infrastructure.</summary>
public interface IWalletTrackerDbContext
{
    DbSet<TrackedWallet> Wallets { get; }
    DbSet<Trade> Trades { get; }
    DbSet<TokenPosition> TokenPositions { get; }
    DbSet<CostBasisLot> CostBasisLots { get; }
    DbSet<WalletStats> WalletStats { get; }
    DbSet<QuotePnL> QuotePnLs { get; }
    DbSet<NewTokenAlert> NewTokenAlerts { get; }
    DbSet<NotificationChannel> NotificationChannels { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
