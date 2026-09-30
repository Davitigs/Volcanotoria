using FluentValidation;
using HazardDashboard.Api.Contracts;
using HazardDashboard.Api.Data;
using HazardDashboard.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HazardDashboard.Api.Controllers;

[ApiController]
[Route("api/volcanoes")]
public sealed class VolcanoesController(HazardDbContext db, IValidator<VolcanoBoundsQuery> boundsValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] VolcanoBoundsQuery query, CancellationToken ct)
    {
        var validation = await boundsValidator.ValidateAsync(query, ct);
        if (!validation.IsValid) return ProblemDetails(validation);

        var volcanoes = db.Volcanoes.AsNoTracking().AsQueryable();
        var all = await volcanoes.OrderBy(v => v.Name).ToListAsync(ct);
        var filtered = all.Where(v =>
            (!query.MinLat.HasValue || v.Location.Y >= query.MinLat) &&
            (!query.MaxLat.HasValue || v.Location.Y <= query.MaxLat) &&
            (!query.MinLon.HasValue || v.Location.X >= query.MinLon) &&
            (!query.MaxLon.HasValue || v.Location.X <= query.MaxLon));
        return Ok(filtered.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var volcano = await db.Volcanoes.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, ct);
        return volcano is null ? NotFoundProblem($"Volcano {id} was not found.") : Ok(ToDto(volcano));
    }

    [HttpGet("{id:int}/hazard-score")]
    public async Task<IActionResult> HazardScore(int id, CancellationToken ct)
    {
        var volcano = await db.Volcanoes.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, ct);
        if (volcano is null) return NotFoundProblem($"Volcano {id} was not found.");

        var cutoff = DateTime.UtcNow.AddDays(-30);
        var events = await db.SeismicEvents.AsNoTracking()
            .Where(e => e.OccurredAt >= cutoff && e.Location.IsWithinDistance(volcano.Location, 50_000))
            .ToListAsync(ct);

        var raw = events.Sum(e =>
            (Math.Min(events.Count, 20) / 20.0) * 35 +
            Math.Clamp(e.Magnitude / 8, 0, 1) * 35 +
            (e.OccurredAt >= DateTime.UtcNow.AddDays(-7) ? 20 : 8) +
            Math.Clamp(1 - e.DepthKm / 100, 0, 1) * 10);
        var score = Math.Min(100, Math.Round(raw, 2));

        return Ok(new
        {
            volcanoId = id,
            score,
            contributingEvents = events.Select(e => e.Id),
            calculatedAt = DateTime.UtcNow,
            methodology = "Heuristic indicator; not a seismological hazard forecast."
        });
    }

    private static object ToDto(Volcano v) => new { v.Id, v.Name, latitude = v.Location.Y, longitude = v.Location.X, v.ElevationMeters, v.Country, v.LastEruptionYear, status = v.Status.ToString() };
    private ObjectResult ProblemDetails(FluentValidation.Results.ValidationResult result) => Problem(statusCode: 400, title: "Validation failed", detail: string.Join(" ", result.Errors.Select(e => e.ErrorMessage)), type: "https://httpstatuses.com/400");
    private ObjectResult NotFoundProblem(string detail) => Problem(statusCode: 404, title: "Not found", detail: detail, type: "https://httpstatuses.com/404");
}
