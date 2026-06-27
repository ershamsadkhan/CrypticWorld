using WalletTracker.Domain;

namespace WalletTracker.Infrastructure.Chains.Evm;

/// <summary>Wrapped-native-token addresses used to identify the "quote" leg of a swap on each EVM chain.</summary>
public static class KnownAddresses
{
    public static readonly Dictionary<Chain, string> WrappedNative = new()
    {
        [Chain.Ethereum] = "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2", // WETH
        [Chain.Bsc] = "0xbb4CdB9CBd36B01bD1cBaEBF2De08d9173bc095c",     // WBNB
        [Chain.Base] = "0x4200000000000000000000000000000000000006"   // WETH (Base)
    };
}
