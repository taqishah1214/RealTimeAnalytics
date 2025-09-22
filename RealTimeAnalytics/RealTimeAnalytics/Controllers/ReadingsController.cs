
using Microsoft.AspNetCore.Mvc;
using RealtimeAnalytics.Api.Services.Abstractions;

namespace RealtimeAnalytics.Api.Controllers;

[ApiController]
[Route("api/readings")]
public class ReadingsController : ControllerBase
{
    private readonly ITimeSeriesStore _store;
    public ReadingsController(ITimeSeriesStore store) { _store = store; }

    [HttpGet("recent")]
    public IActionResult Recent([FromQuery]int sensorId, [FromQuery]int windowSeconds = 300)
    {
        var data = _store.GetWindow(sensorId, TimeSpan.FromSeconds(windowSeconds));
        return Ok(data.Select(d => new { d.SensorId, ts = d.Timestamp.ToUnixTimeMilliseconds(), d.Value }));
    }

    [HttpGet("stats")]
    public IActionResult Stats([FromQuery]int sensorId, [FromQuery]int windowSeconds = 300)
    {
        var s = _store.GetStats(sensorId, TimeSpan.FromSeconds(windowSeconds));
        return Ok(new { sensorId, s.Min, s.Max, s.Mean, s.StdDev, s.Count });
    }
}
