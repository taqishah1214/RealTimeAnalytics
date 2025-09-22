
namespace RealtimeAnalytics.Domain;

public class Sensor
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = "°C";
    public bool IsActive { get; set; } = true;
}
