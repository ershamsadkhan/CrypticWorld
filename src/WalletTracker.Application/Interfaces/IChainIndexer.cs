using WalletTracker.Domain;

namespace WalletTracker.Application.Interfaces;

public interface IChainIndexer
{
    Chain Chain { get; }
    Task PollNewActivityAsync(TrackedWallet wallet, CancellationToken ct);

    /// <summary>One-time full historical scan from genesis/oldest-available data, triggered explicitly
    /// (e.g. via an API call) rather than on the regular incremental polling cadence.</summary>
    Task BackfillAsync(TrackedWallet wallet, CancellationToken ct);
}
