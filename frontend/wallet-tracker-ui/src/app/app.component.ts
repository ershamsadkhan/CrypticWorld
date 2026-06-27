import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { SignalrService } from './services/signalr.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Wallet Tracker';

  // Injecting eagerly starts the SignalR connection as soon as the app loads.
  constructor(private signalr: SignalrService) {}
}
