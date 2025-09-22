
using Microsoft.EntityFrameworkCore;
using RealtimeAnalytics.Domain;
using RealtimeAnalytics.Infrastructure;

namespace RealtimeAnalytics.Api.Services;

public class AlertEvaluator
{
    private readonly AppDbContext _db;
    private readonly ILogger<AlertEvaluator> _logger;
    private readonly double _sigma;

    public AlertEvaluator(AppDbContext db, ILogger<AlertEvaluator> logger, IConfiguration cfg)
    {
        _db = db; _logger = logger;
        _sigma = cfg.GetValue("Anomaly:Sigma", 3.0);
    }

    public async Task CheckAsync(SensorReading reading, (double Min,double Max,double Mean,double StdDev,long Count) stats, CancellationToken ct)
    {
        if (stats.Count < 30) return; // warmup
        if (stats.StdDev <= 0) return;
        var z = Math.Abs((reading.Value - stats.Mean) / stats.StdDev);
        if (z >= _sigma)
        {
            var alert = new Alert
            {
                SensorId = reading.SensorId,
                Timestamp = reading.Timestamp,
                Type = "ZScore",
                Message = $"Anomaly detected (z={z:F2})",
                Value = reading.Value,
                Mean = stats.Mean,
                StdDev = stats.StdDev
            };
            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("Alert for sensor {SensorId}: {Msg}", reading.SensorId, alert.Message);
        }
    }
}
