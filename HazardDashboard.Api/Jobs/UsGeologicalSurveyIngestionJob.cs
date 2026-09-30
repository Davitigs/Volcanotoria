using System.Net.Http.Json;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using HazardDashboard.Api.Data;
using HazardDashboard.Api.Domain;
using HazardDashboard.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HazardDashboard.Api.Jobs;

public sealed class UsGeologicalSurveyIngestionJob(
    IHttpClientFactory httpClientFactory,
    IDbContextFactory<HazardDbContext> dbContextFactory,
    IHubContext<HazardHub> hubContext,
    ILogger<UsGeologicalSurveyIngestionJob> logger)
{
    private const string FeedUrl = "https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_day.geojson";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 300])]
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClientFactory.CreateClient("usgs").GetAsync(FeedUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var processed = 0;
            var newEvents = new List<object>();

            foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
            {
                var mapped = MapFeature(feature);
                if (mapped is null) continue;

                var existing = await db.SeismicEvents.FindAsync([mapped.Id], cancellationToken);
                if (existing is null)
                {
                    db.SeismicEvents.Add(mapped);
                    newEvents.Add(ToClientEvent(mapped));
                }
                else
                {
                    existing.Magnitude = mapped.Magnitude;
                    existing.Location = mapped.Location;
                    existing.DepthKm = mapped.DepthKm;
                    existing.OccurredAt = mapped.OccurredAt;
                    existing.Place = mapped.Place;
                    existing.IngestedAt = mapped.IngestedAt;
                }

                processed++;
            }

            await db.SaveChangesAsync(cancellationToken);
            if (newEvents.Count > 0)
                await hubContext.Clients.All.SendAsync("NewSeismicEvents", newEvents, cancellationToken);
            logger.LogInformation("USGS ingestion completed; processed {EventCount} events", processed);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "USGS feed unavailable; skipping this ingestion cycle");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "USGS feed JSON was invalid; skipping this ingestion cycle");
        }
    }

    private static object ToClientEvent(SeismicEvent e) => new
    {
        e.Id,
        e.Magnitude,
        latitude = e.Location.Y,
        longitude = e.Location.X,
        e.DepthKm,
        e.OccurredAt,
        e.Place,
        e.Source,
        e.IngestedAt
    };

    private static SeismicEvent? MapFeature(JsonElement feature)
    {
        if (!feature.TryGetProperty("id", out var idElement) || idElement.GetString() is not { Length: > 0 } id)
            return null;

        var properties = feature.GetProperty("properties");
        var geometry = feature.GetProperty("geometry");
        var coordinates = geometry.GetProperty("coordinates");
        if (coordinates.GetArrayLength() < 3 || coordinates[0].ValueKind != JsonValueKind.Number)
            return null;

        var longitude = coordinates[0].GetDouble();
        var latitude = coordinates[1].GetDouble();
        var depthKm = coordinates[2].GetDouble();
        if (!properties.TryGetProperty("time", out var timeElement) || timeElement.ValueKind != JsonValueKind.Number)
            return null;

        var occurredAt = DateTimeOffset.FromUnixTimeMilliseconds(timeElement.GetInt64()).UtcDateTime;
        var magnitude = properties.TryGetProperty("mag", out var mag) && mag.ValueKind == JsonValueKind.Number
            ? mag.GetDouble() : 0;
        var place = properties.TryGetProperty("place", out var placeElement)
            ? placeElement.GetString() ?? "Unknown location" : "Unknown location";

        return new SeismicEvent
        {
            Id = id,
            Magnitude = magnitude,
            Location = NtsGeometryServices.Instance.CreateGeometryFactory(4326).CreatePoint(new Coordinate(longitude, latitude)),
            DepthKm = depthKm,
            OccurredAt = occurredAt,
            Place = place,
            Source = "USGS",
            IngestedAt = DateTime.UtcNow
        };
    }
}
