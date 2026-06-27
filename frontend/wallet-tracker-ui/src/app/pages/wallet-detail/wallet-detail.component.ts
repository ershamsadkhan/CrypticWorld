import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { WalletApiService } from '../../services/wallet-api.service';
import { SignalrService } from '../../services/signalr.service';
import { NewTokenAlertDto, TradeDirection, TradeDto, WalletStatsDto } from '../../models';

@Component({
  selector: 'app-wallet-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './wallet-detail.component.html',
  styleUrl: './wallet-detail.component.css'
})
export class WalletDetailComponent implements OnInit, OnDestroy {
  walletId!: number;
  trades: TradeDto[] = [];
  stats: WalletStatsDto | null = null;
  alerts: NewTokenAlertDto[] = [];

  TradeDirection = TradeDirection;

  private subscriptions = new Subscription();

  constructor(
    private route: ActivatedRoute,
    private walletApi: WalletApiService,
    private signalr: SignalrService
  ) {}

  ngOnInit(): void {
    this.walletId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAll();

    this.subscriptions.add(
      this.signalr.trade$.subscribe((trade) => {
        if (trade.walletId === this.walletId) {
          this.trades = [trade, ...this.trades];
          this.loadStats();
        }
      })
    );

    this.subscriptions.add(
      this.signalr.newToken$.subscribe((alert) => {
        if (alert.walletId === this.walletId) {
          this.loadAlerts();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadAll(): void {
    this.walletApi.getTrades(this.walletId).subscribe((trades) => (this.trades = trades));
    this.loadStats();
    this.loadAlerts();
  }

  private loadStats(): void {
    this.walletApi.getStats(this.walletId).subscribe({
      next: (stats) => (this.stats = stats),
      // 404 just means this wallet has no trades indexed yet; leave stats as null.
      error: () => (this.stats = null)
    });
  }

  private loadAlerts(): void {
    this.walletApi.getAlerts(this.walletId).subscribe((alerts) => (this.alerts = alerts));
  }
}
