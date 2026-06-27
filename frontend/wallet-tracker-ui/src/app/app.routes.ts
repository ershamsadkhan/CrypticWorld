import { Routes } from '@angular/router';
import { WalletListComponent } from './pages/wallet-list/wallet-list.component';
import { WalletDetailComponent } from './pages/wallet-detail/wallet-detail.component';
import { NotificationSettingsComponent } from './pages/notification-settings/notification-settings.component';

export const routes: Routes = [
  { path: '', component: WalletListComponent },
  { path: 'wallets/:id', component: WalletDetailComponent },
  { path: 'notifications', component: NotificationSettingsComponent },
  { path: '**', redirectTo: '' }
];
