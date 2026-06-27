import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateWalletRequest,
  NewTokenAlertDto,
  TradeDto,
  WalletDto,
  WalletStatsDto
} from '../models';
import { API_BASE_URL } from '../app.tokens';

@Injectable({ providedIn: 'root' })
export class WalletApiService {
  private readonly baseUrl = `${API_BASE_URL}/api/wallets`;

  constructor(private http: HttpClient) {}

  getWallets(): Observable<WalletDto[]> {
    return this.http.get<WalletDto[]>(this.baseUrl);
  }

  addWallet(request: CreateWalletRequest): Observable<WalletDto> {
    return this.http.post<WalletDto>(this.baseUrl, request);
  }

  deleteWallet(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getTrades(walletId: number, page = 1, pageSize = 50): Observable<TradeDto[]> {
    return this.http.get<TradeDto[]>(`${this.baseUrl}/${walletId}/trades`, {
      params: { page, pageSize }
    });
  }

  getStats(walletId: number): Observable<WalletStatsDto> {
    return this.http.get<WalletStatsDto>(`${this.baseUrl}/${walletId}/stats`);
  }

  getAlerts(walletId: number): Observable<NewTokenAlertDto[]> {
    return this.http.get<NewTokenAlertDto[]>(`${this.baseUrl}/${walletId}/alerts`);
  }

  backfill(walletId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${walletId}/backfill`, {});
  }
}
