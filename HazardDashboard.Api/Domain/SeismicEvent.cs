using NetTopologySuite.Geometries;

namespace HazardDashboard.Api.Domain;

public sealed class SeismicEvent
{
    public required string Id { get; set; }
    public double Magnitude { get; set; }
    public Point Location { get; set; } = default!;
    public double DepthKm { get; set; }
    public DateTime OccurredAt { get; set; }
    public required string Place { get; set; }
    public required string Source { get; set; }
    public DateTime IngestedAt { get; set; }
}
