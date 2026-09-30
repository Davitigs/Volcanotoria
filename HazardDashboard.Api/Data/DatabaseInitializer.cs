using System.Text.Json;
using HazardDashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NetTopologySuite;

namespace HazardDashboard.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(HazardDbContext db, IWebHostEnvironment environment, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(ct);
                break;
            }
            catch (NpgsqlException) when (attempt < 12)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
        if (await db.Volcanoes.AnyAsync(ct)) return;

        var path = Path.Combine(environment.ContentRootPath, "Data", "volcanoes.json");
        await using var stream = File.OpenRead(path);
        var records = await JsonSerializer.DeserializeAsync<List<SeedVolcano>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct) ?? [];
        var factory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Volcanoes.AddRange(records.Select(x => new Volcano
        {
            Name = x.Name, Location = factory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(x.Longitude, x.Latitude)), ElevationMeters = x.ElevationMeters,
            Country = x.Country, LastEruptionYear = x.LastEruptionYear,
            Status = Enum.TryParse<VolcanoStatus>(x.Status, true, out var status) ? status : VolcanoStatus.Unknown
        }));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private sealed record SeedVolcano(string Name, double Latitude, double Longitude, double ElevationMeters, string Country, int? LastEruptionYear, string Status);
}
