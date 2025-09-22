
using Microsoft.AspNetCore.SignalR;

namespace RealtimeAnalytics.Api.Hubs;

public interface ITelemetryClient
{
    Task Reading(int sensorId, long timestampMs, double value, double mean, double stddev);
    Task Alert(object alert);
}

public class TelemetryHub : Hub<ITelemetryClient> { }
