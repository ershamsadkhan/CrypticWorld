import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationChannelDto, UpsertNotificationChannelRequest } from '../models';
import { API_BASE_URL } from '../app.tokens';

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  private readonly baseUrl = `${API_BASE_URL}/api/notifications/channels`;

  constructor(private http: HttpClient) {}

  getChannels(): Observable<NotificationChannelDto[]> {
    return this.http.get<NotificationChannelDto[]>(this.baseUrl);
  }

  createChannel(request: UpsertNotificationChannelRequest): Observable<NotificationChannelDto> {
    return this.http.post<NotificationChannelDto>(this.baseUrl, request);
  }

  updateChannel(id: number, request: UpsertNotificationChannelRequest): Observable<NotificationChannelDto> {
    return this.http.put<NotificationChannelDto>(`${this.baseUrl}/${id}`, request);
  }

  deleteChannel(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  sendTest(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/test`, {});
  }
}
