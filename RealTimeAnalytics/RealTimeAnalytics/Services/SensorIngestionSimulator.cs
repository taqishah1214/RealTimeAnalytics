
using System.Threading.Channels;
using RealtimeAnalytics.Domain;

namespace RealtimeAnalytics.Api.Services;

public class SensorIngestionSimulator : BackgroundService
{
    private readonly Channel<SensorReading> _channel;
    private readonly ILogger<SensorIngestionSimulator> _logger;
    private readonly int _sensors;
    private readonly int _rate;
    private readonly Random _rand = new();

    public SensorIngestionSimulator(Channel<SensorReading> channel, ILogger<SensorIngestionSimulator> logger, IConfiguration cfg)
    {
        _channel = channel; _logger = logger;
        _sensors = cfg.GetValue("Simulator:Sensors", 10);
        _rate = cfg.GetValue("Simulator:ReadingsPerSecond", 1000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(1000.0 / _rate);
        while (!stoppingToken.IsCancellationRequested)
        {
            var sensorId = (_rand.Next(_sensors) + 1);
            var baseVal = 25.0 + sensorId; // deterministic baseline per sensor
            var noise = (_rand.NextDouble() - 0.5) * 2.0; // [-1..1]
            var anomaly = _rand.NextDouble() < 0.001 ? (_rand.NextDouble()*15 - 7.5) : 0; // rare spike
            var value = baseVal + noise + anomaly;
            await _channel.Writer.WriteAsync(new SensorReading(sensorId, DateTimeOffset.UtcNow, value), stoppingToken);
            await Task.Delay(delay, stoppingToken);
        }
    }
}
