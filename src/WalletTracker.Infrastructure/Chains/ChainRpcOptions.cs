namespace WalletTracker.Infrastructure.Chains;

public class ChainRpcOptions
{
    public SolanaRpcOptions Solana { get; set; } = new();
    public EvmRpcOptions Ethereum { get; set; } = new();
    public EvmRpcOptions Bsc { get; set; } = new();
    public EvmRpcOptions Base { get; set; } = new();
}

public class SolanaRpcOptions
{
    public List<string> Urls { get; set; } = new();
}

public class EvmRpcOptions
{
    public long ChainId { get; set; }
    public List<string> Urls { get; set; } = new();
}
