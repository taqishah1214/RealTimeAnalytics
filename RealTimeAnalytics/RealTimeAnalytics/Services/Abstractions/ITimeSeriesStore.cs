
using RealtimeAnalytics.Domain;

namespace RealtimeAnalytics.Api.Services.Abstractions;

public interface ITimeSeriesStore
{
    void Add(SensorReading reading);
    IReadOnlyList<SensorReading> GetWindow(int sensorId, TimeSpan window);
    (double Min, double Max, double Mean, double StdDev, long Count) GetStats(int sensorId, TimeSpan window);
    long TotalCount { get; }
}
