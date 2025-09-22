
using System.Collections.Concurrent;
using RealtimeAnalytics.Domain;
using RealtimeAnalytics.Api.Services.Abstractions;

namespace RealtimeAnalytics.Api.Services;

public class TimeSeriesStore : ITimeSeriesStore
{
    private readonly ConcurrentDictionary<int, ConcurrentQueue<SensorReading>> _series = new();
    private long _totalCount = 0;
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public long TotalCount => Interlocked.Read(ref _totalCount);

    public void Add(SensorReading reading)
    {
        var q = _series.GetOrAdd(reading.SensorId, _ => new ConcurrentQueue<SensorReading>());
        q.Enqueue(reading);
        Interlocked.Increment(ref _totalCount);

        // local purge for the sensor to keep <=24h
        while (q.TryPeek(out var head) && (reading.Timestamp - head.Timestamp) > Retention)
        {
            q.TryDequeue(out _);
            Interlocked.Decrement(ref _totalCount);
        }
    }

    public IReadOnlyList<SensorReading> GetWindow(int sensorId, TimeSpan window)
    {
        if (!_series.TryGetValue(sensorId, out var q)) return Array.Empty<SensorReading>();
        var since = DateTimeOffset.UtcNow - window;
        return q.Where(r => r.Timestamp >= since).ToList();
    }

    public (double Min, double Max, double Mean, double StdDev, long Count) GetStats(int sensorId, TimeSpan window)
    {
        var data = GetWindow(sensorId, window);
        if (data.Count == 0) return (0,0,0,0,0);
        double min = double.MaxValue, max = double.MinValue, mean = 0, M2 = 0;
        long n = 0;
        foreach (var d in data)
        {
            n++;
            var x = d.Value;
            if (x < min) min = x;
            if (x > max) max = x;
            var delta = x - mean;
            mean += delta / n;
            M2 += delta * (x - mean);
        }
        var variance = n > 1 ? M2 / (n - 1) : 0;
        return (min, max, mean, Math.Sqrt(variance), n);
    }
}
