import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationApiService } from '../../services/notification-api.service';
import { NotificationChannelDto, NotificationChannelType } from '../../models';

interface TelegramForm {
  botToken: string;
  chatId: string;
}

interface DiscordForm {
  webhookUrl: string;
}

interface WhatsAppForm {
  accountSid: string;
  authToken: string;
  fromNumber: string;
  toNumber: string;
}

@Component({
  selector: 'app-notification-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './notification-settings.component.html',
  styleUrl: './notification-settings.component.css'
})
export class NotificationSettingsComponent implements OnInit {
  channels: NotificationChannelDto[] = [];

  telegram: TelegramForm = { botToken: '', chatId: '' };
  telegramEnabled = false;

  discord: DiscordForm = { webhookUrl: '' };
  discordEnabled = false;

  whatsApp: WhatsAppForm = { accountSid: '', authToken: '', fromNumber: '', toNumber: '' };
  whatsAppEnabled = false;

  statusMessage = '';

  constructor(private notificationApi: NotificationApiService) {}

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.notificationApi.getChannels().subscribe((channels) => {
      this.channels = channels;

      const telegram = channels.find((c) => c.type === NotificationChannelType.Telegram);
      if (telegram) {
        this.telegram = JSON.parse(telegram.configJson);
        this.telegramEnabled = telegram.isEnabled;
      }

      const discord = channels.find((c) => c.type === NotificationChannelType.Discord);
      if (discord) {
        this.discord = JSON.parse(discord.configJson);
        this.discordEnabled = discord.isEnabled;
      }

      const whatsApp = channels.find((c) => c.type === NotificationChannelType.WhatsApp);
      if (whatsApp) {
        this.whatsApp = JSON.parse(whatsApp.configJson);
        this.whatsAppEnabled = whatsApp.isEnabled;
      }
    });
  }

  saveTelegram(): void {
    this.save(NotificationChannelType.Telegram, this.telegram, this.telegramEnabled);
  }

  saveDiscord(): void {
    this.save(NotificationChannelType.Discord, this.discord, this.discordEnabled);
  }

  saveWhatsApp(): void {
    this.save(NotificationChannelType.WhatsApp, this.whatsApp, this.whatsAppEnabled);
  }

  testChannel(type: NotificationChannelType): void {
    const existing = this.channels.find((c) => c.type === type);
    if (!existing) {
      this.statusMessage = 'Save the channel before sending a test.';
      return;
    }
    this.notificationApi.sendTest(existing.id).subscribe({
      next: () => (this.statusMessage = 'Test notification sent.'),
      error: () => (this.statusMessage = 'Failed to send test notification.')
    });
  }

  private save(type: NotificationChannelType, config: unknown, isEnabled: boolean): void {
    const existing = this.channels.find((c) => c.type === type);
    const request = { type, configJson: JSON.stringify(config), isEnabled };

    const obs = existing
      ? this.notificationApi.updateChannel(existing.id, request)
      : this.notificationApi.createChannel(request);

    obs.subscribe({
      next: () => {
        this.statusMessage = 'Saved.';
        this.load();
      },
      error: () => (this.statusMessage = 'Failed to save.')
    });
  }
}
