using WalletTracker.Domain;

namespace WalletTracker.Application.Dtos;

public record CreateWalletRequest(string Address, Chain Chain, string? Label);

public record WalletDto(int Id, string Address, Chain Chain, string? Label, bool IsActive, DateTime CreatedAt, BackfillStatus BackfillStatus);

public record TradeDto(
    long Id, int WalletId, Chain Chain, string TxHash, DateTime BlockTime, TradeDirection Direction,
    string TokenAddress, string? TokenSymbol, decimal AmountToken, decimal AmountQuote, decimal PricePerTokenInQuote,
    string QuoteSymbol, string? DexName);

public record QuotePnLDto(string QuoteSymbol, decimal RealizedPnL, decimal UnrealizedPnL);

public record WalletStatsDto(
    int WalletId, int TotalTrades, int TotalSells, int WinningSells, decimal WinRate,
    List<QuotePnLDto> PnLByQuote, DateTime UpdatedAt);

public record NewTokenAlertDto(long Id, int WalletId, string TokenAddress, string? TokenSymbol, DateTime FirstSeenAt, bool Notified);
