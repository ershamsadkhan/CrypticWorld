namespace WalletTracker.Domain;

public enum Chain
{
    Solana = 0,
    Ethereum = 1,
    Bsc = 2,
    Base = 3
}

public enum TradeDirection
{
    Buy = 0,
    Sell = 1
}

public enum NotificationChannelType
{
    Telegram = 0,
    Discord = 1,
    WhatsApp = 2
}

public enum BackfillStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}
