
using Microsoft.EntityFrameworkCore;
using RealtimeAnalytics.Domain;

namespace RealtimeAnalytics.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sensor>(e => {
            e.ToTable("Sensors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(32).HasDefaultValue("°C");
        });

        modelBuilder.Entity<Alert>(e => {
            e.ToTable("Alerts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(64).IsRequired();
            e.Property(x => x.Message).HasMaxLength(512).IsRequired();
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => new { x.SensorId, x.Timestamp });
        });
    }
}
