export enum Chain {
  Solana = 0,
  Ethereum = 1,
  Bsc = 2,
  Base = 3
}

export enum TradeDirection {
  Buy = 0,
  Sell = 1
}

export enum NotificationChannelType {
  Telegram = 0,
  Discord = 1,
  WhatsApp = 2
}

export enum BackfillStatus {
  NotStarted = 0,
  InProgress = 1,
  Completed = 2,
  Failed = 3
}

export interface WalletDto {
  id: number;
  address: string;
  chain: Chain;
  label?: string;
  isActive: boolean;
  createdAt: string;
  backfillStatus: BackfillStatus;
}

export interface CreateWalletRequest {
  address: string;
  chain: Chain;
  label?: string;
}

export interface TradeDto {
  id: number;
  walletId: number;
  chain: Chain;
  txHash: string;
  blockTime: string;
  direction: TradeDirection;
  tokenAddress: string;
  tokenSymbol?: string;
  amountToken: number;
  amountQuote: number;
  pricePerTokenInQuote: number;
  dexName?: string;
}

export interface WalletStatsDto {
  walletId: number;
  totalTrades: number;
  totalSells: number;
  winningSells: number;
  winRate: number;
  realizedPnLInQuote: number;
  unrealizedPnLInQuote: number;
  updatedAt: string;
}

export interface NewTokenAlertDto {
  id: number;
  walletId: number;
  tokenAddress: string;
  tokenSymbol?: string;
  firstSeenAt: string;
  notified: boolean;
}

export interface NotificationChannelDto {
  id: number;
  type: NotificationChannelType;
  configJson: string;
  isEnabled: boolean;
  updatedAt: string;
}

export interface UpsertNotificationChannelRequest {
  type: NotificationChannelType;
  configJson: string;
  isEnabled: boolean;
}
