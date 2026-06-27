namespace WalletTracker.Infrastructure.Chains.Evm;

public static class Erc20Abi
{
    public const string MinimalAbi = """
    [
        { "constant": true, "inputs": [], "name": "decimals", "outputs": [{ "name": "", "type": "uint8" }], "type": "function" },
        { "constant": true, "inputs": [], "name": "symbol", "outputs": [{ "name": "", "type": "string" }], "type": "function" }
    ]
    """;
}
