
using System;

namespace RealtimeAnalytics.Domain;

public class Alert
{
    public long Id { get; set; }
    public int SensorId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Type { get; set; } = "Anomaly";
    public string Message { get; set; } = default!;
    public double Value { get; set; }
    public double Mean { get; set; }
    public double StdDev { get; set; }
}
