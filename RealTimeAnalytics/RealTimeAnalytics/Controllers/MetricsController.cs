
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealtimeAnalytics.Api.Services.Abstractions;
using RealtimeAnalytics.Infrastructure;

namespace RealtimeAnalytics.Api.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly ITimeSeriesStore _store;
    private readonly AppDbContext _db;

    public MetricsController(ITimeSeriesStore store, AppDbContext db) { _store = store; _db = db; }

    [HttpGet("summary")]
    public IActionResult Summary([FromQuery]int sensorId, [FromQuery] int windowSeconds = 60)
    {
        var stats = _store.GetStats(sensorId, TimeSpan.FromSeconds(windowSeconds));
        return Ok(new { sensorId, windowSeconds, stats.Min, stats.Max, stats.Mean, stats.StdDev, stats.Count });
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts([FromQuery]int? sensorId = null, [FromQuery]int limit = 50)
    {
        var q = _db.Alerts.AsNoTracking().OrderByDescending(a => a.Timestamp);
        if (sensorId.HasValue) q = q.Where(a => a.SensorId == sensorId).OrderByDescending(a => a.Timestamp);
        var items = await q.Take(limit).ToListAsync();
        return Ok(items);
    }
}
