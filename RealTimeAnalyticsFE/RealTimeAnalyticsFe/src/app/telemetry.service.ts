
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class TelemetryService {
  hubUrl = 'https://localhost:7244/hubs/telemetry';
  private connection?: signalR.HubConnection;
  private readingHandlers: Array<(sid:number, ts:number, value:number, m:number, s:number)=>void> = [];

  connect() {
    if (this.connection) return;
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { withCredentials: false })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.on('Reading', (sid:number, ts:number, value:number, mean:number, std:number) => {
      for (const h of this.readingHandlers) h(sid, ts, value, mean, std);
    });

    this.connection.start().catch(console.error);
  }

  onReading(handler: (sid:number, ts:number, value:number, m:number, s:number)=>void) {
    this.readingHandlers.push(handler);
  }

  disconnect() {
    if (this.connection) {
      this.connection.stop();
      this.connection = undefined;
    }
  }
}
