using NetTopologySuite.Geometries;

namespace HazardDashboard.Api.Domain;

public enum VolcanoStatus { Active, Dormant, Extinct, Unknown }

public sealed class Volcano
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public Point Location { get; set; } = default!;
    public double ElevationMeters { get; set; }
    public required string Country { get; set; }
    public int? LastEruptionYear { get; set; }
    public VolcanoStatus Status { get; set; }
}
