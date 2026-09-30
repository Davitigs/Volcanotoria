using FluentValidation;
using HazardDashboard.Api.Contracts;
using HazardDashboard.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;

namespace HazardDashboard.Api.Controllers;

[ApiController]
[Route("api/seismic-events")]
public sealed class SeismicEventsController(HazardDbContext db, IValidator<SeismicEventsQuery> eventsValidator, IValidator<NearbyEventsQuery> nearbyValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SeismicEventsQuery query, CancellationToken ct)
    {
        var validation = await eventsValidator.ValidateAsync(query, ct);
        if (!validation.IsValid) return Problem(statusCode: 400, title: "Validation failed", detail: string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var events = db.SeismicEvents.AsNoTracking().AsQueryable();
        if (query.Since.HasValue) events = events.Where(e => e.OccurredAt >= query.Since.Value.ToUniversalTime());
        if (query.MinMagnitude.HasValue) events = events.Where(e => e.Magnitude >= query.MinMagnitude.Value);
        var rows = await events.OrderByDescending(e => e.OccurredAt).ToListAsync(ct);
        return Ok(rows.Select(e => new { e.Id, e.Magnitude, latitude = e.Location.Y, longitude = e.Location.X, e.DepthKm, e.OccurredAt, e.Place, e.Source, e.IngestedAt }));
    }

    [HttpGet("near")]
    public async Task<IActionResult> Near([FromQuery] NearbyEventsQuery query, CancellationToken ct)
    {
        var validation = await nearbyValidator.ValidateAsync(query, ct);
        if (!validation.IsValid) return Problem(statusCode: 400, title: "Validation failed", detail: string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var point = NtsGeometryServices.Instance.CreateGeometryFactory(4326).CreatePoint(new NetTopologySuite.Geometries.Coordinate(query.Lon, query.Lat));
        var events = await db.SeismicEvents.AsNoTracking()
            .Where(e => e.Location.IsWithinDistance(point, query.RadiusKm * 1000))
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
        return Ok(events.Select(e => new { e.Id, e.Magnitude, latitude = e.Location.Y, longitude = e.Location.X, e.DepthKm, e.OccurredAt, e.Place }));
    }
}
