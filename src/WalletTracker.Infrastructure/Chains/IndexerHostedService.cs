using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using WalletTracker.Application.Interfaces;
using WalletTracker.Domain;

namespace WalletTracker.Infrastructure.Chains;

/// <summary>
/// Background service that, on a fixed interval, loads all active TrackedWallets and dispatches
/// each to the IChainIndexer matching its chain. Runs inside the Web API process for v1.
/// </summary>
public class IndexerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndexerHostedService> _logger;
    private readonly TimeSpan _pollInterval;

    public IndexerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<IndexerHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = configuration.GetValue<int?>("IndexerPollingIntervalSeconds") ?? 15;
        _pollInterval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllWalletsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Indexer poll cycle failed");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task PollAllWalletsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IWalletTrackerDbContext>();
        var indexers = scope.ServiceProvider.GetServices<IChainIndexer>().ToList();

        var wallets = await db.Wallets.Where(w => w.IsActive).ToListAsync(ct);

        foreach (var wallet in wallets)
        {
            ct.ThrowIfCancellationRequested();
            var indexer = indexers.FirstOrDefault(i => i.Chain == wallet.Chain);
            if (indexer is null)
            {
                _logger.LogWarning("No indexer registered for chain {Chain}", wallet.Chain);
                continue;
            }

            try
            {
                await indexer.PollNewActivityAsync(wallet, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed polling wallet {Address} on {Chain}", wallet.Address, wallet.Chain);
            }
        }
    }
}
