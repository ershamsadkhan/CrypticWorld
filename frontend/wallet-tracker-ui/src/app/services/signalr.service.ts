import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { TradeDto } from '../models';
import { API_BASE_URL } from '../app.tokens';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection: signalR.HubConnection;

  readonly trade$ = new Subject<TradeDto>();
  readonly newToken$ = new Subject<{ walletId: number; tokenAddress: string; tokenSymbol?: string }>();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/wallet`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('trade', (trade: TradeDto) => this.trade$.next(trade));
    this.connection.on('newToken', (alert) => this.newToken$.next(alert));

    this.connection.start().catch((err) => console.error('SignalR connection failed', err));
  }
}
