namespace HazardDashboard.Api.Contracts;

public sealed class VolcanoBoundsQuery
{
    public double? MinLat { get; set; }
    public double? MinLon { get; set; }
    public double? MaxLat { get; set; }
    public double? MaxLon { get; set; }
}

public sealed class SeismicEventsQuery
{
    public DateTime? Since { get; set; }
    public double? MinMagnitude { get; set; }
}

public sealed class NearbyEventsQuery
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double RadiusKm { get; set; }
}
