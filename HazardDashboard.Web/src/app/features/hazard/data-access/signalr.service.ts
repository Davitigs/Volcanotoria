import { Injectable, signal } from '@angular/core';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { SeismicEvent } from '@hazard/contracts';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  readonly newEvents = signal<SeismicEvent[]>([]);
  readonly connected = signal(false);
  readonly error = signal<string | null>(null);
  private connection?: HubConnection;

  async connect(): Promise<void> {
    this.connection = new HubConnectionBuilder()
      .withUrl('http://localhost:5001/hubs/hazard')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection.on('NewSeismicEvents', (events: SeismicEvent[]) =>
      this.newEvents.update((current) => [...current, ...events]),
    );
    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose((error) => {
      this.connected.set(false);
      this.error.set(error?.message ?? 'SignalR connection closed');
    });
    try {
      await this.connection.start();
      this.connected.set(true);
      this.error.set(null);
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'Unable to connect to live updates');
    }
  }
}
