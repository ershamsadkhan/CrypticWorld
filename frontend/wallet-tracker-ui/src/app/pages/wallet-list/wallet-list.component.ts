import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { WalletApiService } from '../../services/wallet-api.service';
import { BackfillStatus, Chain, WalletDto } from '../../models';

@Component({
  selector: 'app-wallet-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './wallet-list.component.html',
  styleUrl: './wallet-list.component.css'
})
export class WalletListComponent implements OnInit {
  wallets: WalletDto[] = [];
  chains = [
    { value: Chain.Solana, label: 'Solana' },
    { value: Chain.Ethereum, label: 'Ethereum' },
    { value: Chain.Bsc, label: 'BSC' },
    { value: Chain.Base, label: 'Base' }
  ];

  newAddress = '';
  newChain: Chain = Chain.Solana;
  newLabel = '';
  errorMessage = '';

  BackfillStatus = BackfillStatus;

  constructor(private walletApi: WalletApiService) {}

  ngOnInit(): void {
    this.loadWallets();
  }

  loadWallets(): void {
    this.walletApi.getWallets().subscribe((wallets) => (this.wallets = wallets));
  }

  addWallet(): void {
    if (!this.newAddress.trim()) return;
    this.errorMessage = '';

    this.walletApi
      .addWallet({ address: this.newAddress.trim(), chain: this.newChain, label: this.newLabel.trim() || undefined })
      .subscribe({
        next: () => {
          this.newAddress = '';
          this.newLabel = '';
          this.loadWallets();
        },
        error: (err) => {
          this.errorMessage = err?.error ?? 'Failed to add wallet.';
        }
      });
  }

  removeWallet(id: number): void {
    this.walletApi.deleteWallet(id).subscribe(() => this.loadWallets());
  }

  backfill(id: number): void {
    this.walletApi.backfill(id).subscribe({
      next: () => this.loadWallets(),
      error: (err) => (this.errorMessage = err?.error ?? 'Failed to start backfill.')
    });
  }

  chainLabel(chain: Chain): string {
    return this.chains.find((c) => c.value === chain)?.label ?? 'Unknown';
  }

  backfillLabel(status: BackfillStatus): string {
    switch (status) {
      case BackfillStatus.InProgress: return 'Backfilling…';
      case BackfillStatus.Completed: return 'History loaded';
      case BackfillStatus.Failed: return 'Backfill failed (retry)';
      default: return 'Load full history';
    }
  }
}
