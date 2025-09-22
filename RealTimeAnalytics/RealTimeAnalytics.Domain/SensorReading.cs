
using System;

namespace RealtimeAnalytics.Domain;

// perf-friendly struct for in-memory timeseries
public readonly record struct SensorReading(int SensorId, DateTimeOffset Timestamp, double Value);
