
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using RealtimeAnalytics.Domain;
using RealtimeAnalytics.Api.Services.Abstractions;
using RealtimeAnalytics.Infrastructure;
using RealtimeAnalytics.Api.Hubs;

namespace RealtimeAnalytics.Api.Services;

public class ReadingProcessor : BackgroundService
{
    private readonly Channel<SensorReading> _channel;
    private readonly ITimeSeriesStore _store;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TelemetryHub, ITelemetryClient> _hub;

    public ReadingProcessor(Channel<SensorReading> channel, ITimeSeriesStore store, IServiceScopeFactory scopeFactory, IHubContext<TelemetryHub, ITelemetryClient> hub)
    {
        _channel = channel; _store = store; _store = store; _hub = hub; _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_channel.Reader.TryRead(out var reading))
            {
                _store.Add(reading);
                var stats = _store.GetStats(reading.SensorId, TimeSpan.FromMinutes(1));
                // create a scope for AlertEvaluator (scoped service)
                using (var scope = _scopeFactory.CreateScope())
                {
                    var alerts = scope.ServiceProvider.GetRequiredService<AlertEvaluator>();
                    _ = alerts.CheckAsync(reading, stats, stoppingToken);
                }
                // broadcast throttled updates (per reading is fine at 1k/s for demo)
                await _hub.Clients.All.Reading(reading.SensorId, reading.Timestamp.ToUnixTimeMilliseconds(), reading.Value, stats.Mean, stats.StdDev);
            }
        }
    }
}
