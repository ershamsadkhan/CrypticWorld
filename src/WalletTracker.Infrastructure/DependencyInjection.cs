using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletTracker.Application.Interfaces;
using WalletTracker.Application.Services;
using WalletTracker.Domain;
using WalletTracker.Infrastructure.Chains;
using WalletTracker.Infrastructure.Chains.Evm;
using WalletTracker.Infrastructure.Chains.Solana;
using WalletTracker.Infrastructure.Notifications;
using WalletTracker.Infrastructure.Persistence;
using WalletTracker.Infrastructure.Realtime;

namespace WalletTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WalletTrackerDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("WalletTrackerDb")));
        services.AddScoped<IWalletTrackerDbContext>(sp => sp.GetRequiredService<WalletTrackerDbContext>());

        services.Configure<ChainRpcOptions>(configuration.GetSection("ChainRpc"));

        services.AddScoped<TradePersistenceService>();
        services.AddScoped<NotificationDispatcher>();
        services.AddSignalR();
        services.AddScoped<WalletEventBroadcaster>();

        services.AddScoped<IChainIndexer, SolanaIndexer>();
        services.AddScoped<IChainIndexer>(sp => ActivatorUtilities.CreateInstance<EvmIndexer>(sp, Chain.Ethereum));
        services.AddScoped<IChainIndexer>(sp => ActivatorUtilities.CreateInstance<EvmIndexer>(sp, Chain.Bsc));
        services.AddScoped<IChainIndexer>(sp => ActivatorUtilities.CreateInstance<EvmIndexer>(sp, Chain.Base));

        services.AddScoped<INotificationSender, TelegramSender>();
        services.AddScoped<INotificationSender, DiscordSender>();
        services.AddScoped<INotificationSender, WhatsAppSender>();

        services.AddHttpClient();
        services.AddHostedService<IndexerHostedService>();

        return services;
    }
}
