
using Microsoft.EntityFrameworkCore;
using RealtimeAnalytics.Domain;

namespace RealtimeAnalytics.Infrastructure.Seed;

public static class SensorSeed
{
    public static async Task EnsureSeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
        if (!await db.Sensors.AnyAsync())
        {
            for (int i = 1; i <= 10; i++)
                db.Sensors.Add(new Sensor { Id = i, Name = $"Sensor-{i:00}", Unit = "°C", IsActive = true });
            await db.SaveChangesAsync();
        }
    }
}
