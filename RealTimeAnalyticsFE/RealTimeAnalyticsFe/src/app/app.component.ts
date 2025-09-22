
import { Component, OnDestroy, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

import { provideCharts, withDefaultRegisterables, BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { TelemetryService } from './telemetry.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  providers: [provideCharts(withDefaultRegisterables())],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit, OnDestroy {
  sensors = Array.from({ length: 10 }, (_, i) => i + 1);
  readonly hubUrl: string;

  // Default to All sensors so any incoming data is shown
  sensorId = signal<number>(0);
  points = signal<{ t: number; v: number }[]>([]);
  mean = signal(0);
  std = signal(0);
  stats = signal<{ t: number; m: number; s: number }[]>([]);

  chartOptions: ChartConfiguration['options'] = {
    responsive: true,
    animation: false,
    parsing: false,
    normalized: true,
    scales: {
      // Use a linear scale and format ticks as time strings to avoid needing a date adapter
      x: {
        type: 'linear',
        ticks: {
          callback: (value) => {
            const v = typeof value === 'string' ? Number(value) : (value as number);
            const d = new Date(v);
            return d.toLocaleTimeString();
          }
        }
      },
      y: { beginAtZero: false }
    },
    plugins: {
      legend: { display: true },
      decimation: { enabled: true, algorithm: 'lttb', samples: 500 }
    }
  };

  chartData = computed<ChartConfiguration['data']>(() => ({
    datasets: [
      {
        label: 'Mean +1σ',
        data: this.stats().map(p => ({ x: p.t, y: p.m + p.s })),
        pointRadius: 0,
        borderWidth: 1,
        tension: 0.2,
        borderColor: 'rgba(255,152,0,0.6)'
      },
      {
        label: 'Mean -1σ',
        data: this.stats().map(p => ({ x: p.t, y: p.m - p.s })),
        pointRadius: 0,
        borderWidth: 1,
        tension: 0.2,
        borderColor: 'rgba(255,152,0,0.6)',
        backgroundColor: 'rgba(255,152,0,0.15)',
        fill: '-1' as any
      },
      {
        label: 'Value',
        data: this.points().map(p => ({ x: p.t, y: p.v })),
        pointRadius: 0,
        borderWidth: 1,
        tension: 0.2,
        borderColor: '#1976d2',
        backgroundColor: 'rgba(25,118,210,0.15)'
      },
      {
        label: 'Mean',
        data: this.stats().map(p => ({ x: p.t, y: p.m })),
        pointRadius: 0,
        borderWidth: 1,
        tension: 0.2,
        borderColor: '#ff9800'
      }
    ]
  }));
  currentTemp = signal(0);

  constructor(private telemetry: TelemetryService) {
    this.hubUrl = this.telemetry.hubUrl;
  }

  ngOnInit() {
    this.telemetry.connect();
    // Buffer incoming updates and flush at ~5Hz to reduce re-renders
    let bufPoints: { t: number; v: number }[] = [];
    let bufStats: { t: number; m: number; s: number }[] = [];
    let scheduled = false;
    const flush = () => {
      scheduled = false;
      if (bufPoints.length === 0 && bufStats.length === 0) return;
      const cutoff = Date.now() - 60_000; // 1-minute window
      const mergedPts = [...this.points(), ...bufPoints]
        .filter(p => p.t >= cutoff)
        .slice(-5000);
      const mergedStats = [...this.stats(), ...bufStats]
        .filter(p => p.t >= cutoff)
        .slice(-5000);
      this.points.set(mergedPts);
      this.stats.set(mergedStats);
      bufPoints = [];
      bufStats = [];
    };
    const schedule = () => {
      if (!scheduled) {
        scheduled = true;
        setTimeout(flush, 200); // ~5Hz
      }
    };
    this.telemetry.onReading((sid, ts, value, m, s) => {
      // If 'All' (0) is selected, accept any sensor id; otherwise filter by selected id
      if (this.sensorId() !== 0 && sid !== this.sensorId()) return;
      // Normalize timestamp: if seconds, convert to milliseconds
      const tsMs = ts < 2e10 ? ts * 1000 : ts;
      bufPoints.push({ t: tsMs, v: value });
      bufStats.push({ t: tsMs, m, s });
      this.mean.set(m);
      this.std.set(s);
      this.currentTemp.set(value);
      schedule();
    });
  }

  ngOnDestroy() { this.telemetry.disconnect(); }

  changeSensor(val: string) {
    this.sensorId.set(parseInt(val, 10));
  this.points.set([]);
  this.stats.set([]);
  this.mean.set(0);
  this.std.set(0);
  }
}
