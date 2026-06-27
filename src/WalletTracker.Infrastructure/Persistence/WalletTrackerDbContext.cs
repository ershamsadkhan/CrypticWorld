using Microsoft.EntityFrameworkCore;
using WalletTracker.Application.Interfaces;
using WalletTracker.Domain;

namespace WalletTracker.Infrastructure.Persistence;

public class WalletTrackerDbContext : DbContext, IWalletTrackerDbContext
{
    public WalletTrackerDbContext(DbContextOptions<WalletTrackerDbContext> options) : base(options)
    {
    }

    public DbSet<TrackedWallet> Wallets => Set<TrackedWallet>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<TokenPosition> TokenPositions => Set<TokenPosition>();
    public DbSet<CostBasisLot> CostBasisLots => Set<CostBasisLot>();
    public DbSet<WalletStats> WalletStats => Set<WalletStats>();
    public DbSet<QuotePnL> QuotePnLs => Set<QuotePnL>();
    public DbSet<NewTokenAlert> NewTokenAlerts => Set<NewTokenAlert>();
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackedWallet>(e =>
        {
            e.HasIndex(w => new { w.Address, w.Chain }).IsUnique();
            e.Property(w => w.Address).HasMaxLength(128).IsRequired();
            e.HasOne(w => w.Stats).WithOne(s => s.Wallet!).HasForeignKey<WalletStats>(s => s.WalletId);
        });

        modelBuilder.Entity<Trade>(e =>
        {
            e.HasIndex(t => new { t.WalletId, t.TokenAddress });
            e.HasIndex(t => new { t.WalletId, t.TxHash }).IsUnique();
            e.Property(t => t.TxHash).HasMaxLength(128).IsRequired();
            e.Property(t => t.TokenAddress).HasMaxLength(128).IsRequired();
            e.Property(t => t.QuoteSymbol).HasMaxLength(32).IsRequired();
            e.Property(t => t.AmountToken).HasColumnType("decimal(38,18)");
            e.Property(t => t.AmountQuote).HasColumnType("decimal(38,18)");
            e.Property(t => t.PricePerTokenInQuote).HasColumnType("decimal(38,18)");
            e.HasOne(t => t.Wallet).WithMany(w => w.Trades).HasForeignKey(t => t.WalletId);
        });

        modelBuilder.Entity<TokenPosition>(e =>
        {
            e.HasIndex(p => new { p.WalletId, p.TokenAddress, p.QuoteSymbol }).IsUnique();
            e.Property(p => p.TokenAddress).HasMaxLength(128).IsRequired();
            e.Property(p => p.QuoteSymbol).HasMaxLength(32).IsRequired();
            e.Property(p => p.QuantityHeld).HasColumnType("decimal(38,18)");
            e.Property(p => p.LastKnownPriceInQuote).HasColumnType("decimal(38,18)");
            e.HasOne(p => p.Wallet).WithMany(w => w.Positions).HasForeignKey(p => p.WalletId);
        });

        modelBuilder.Entity<CostBasisLot>(e =>
        {
            e.Property(l => l.QuantityRemaining).HasColumnType("decimal(38,18)");
            e.Property(l => l.PricePerTokenInQuote).HasColumnType("decimal(38,18)");
            e.HasOne(l => l.TokenPosition).WithMany(p => p.Lots).HasForeignKey(l => l.TokenPositionId);
        });

        modelBuilder.Entity<WalletStats>(e =>
        {
            e.HasKey(s => s.WalletId);
            e.Property(s => s.WinRate).HasColumnType("decimal(9,4)");
        });

        modelBuilder.Entity<QuotePnL>(e =>
        {
            e.HasIndex(q => new { q.WalletId, q.QuoteSymbol }).IsUnique();
            e.Property(q => q.QuoteSymbol).HasMaxLength(32).IsRequired();
            e.Property(q => q.RealizedPnL).HasColumnType("decimal(38,18)");
            e.Property(q => q.UnrealizedPnL).HasColumnType("decimal(38,18)");
            e.HasOne(q => q.Wallet).WithMany().HasForeignKey(q => q.WalletId);
        });

        modelBuilder.Entity<NewTokenAlert>(e =>
        {
            e.HasIndex(a => new { a.WalletId, a.TokenAddress }).IsUnique();
            e.Property(a => a.TokenAddress).HasMaxLength(128).IsRequired();
            e.HasOne(a => a.Wallet).WithMany().HasForeignKey(a => a.WalletId);
        });

        modelBuilder.Entity<NotificationChannel>(e =>
        {
            e.Property(c => c.ConfigJson).HasColumnType("nvarchar(max)");
        });
    }
}
