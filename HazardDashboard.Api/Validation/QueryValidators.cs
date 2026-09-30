using FluentValidation;
using HazardDashboard.Api.Contracts;

namespace HazardDashboard.Api.Validation;

public sealed class VolcanoBoundsQueryValidator : AbstractValidator<VolcanoBoundsQuery>
{
    public VolcanoBoundsQueryValidator()
    {
        RuleFor(x => x.MinLat).InclusiveBetween(-90, 90).When(x => x.MinLat.HasValue);
        RuleFor(x => x.MaxLat).InclusiveBetween(-90, 90).When(x => x.MaxLat.HasValue);
        RuleFor(x => x.MinLon).InclusiveBetween(-180, 180).When(x => x.MinLon.HasValue);
        RuleFor(x => x.MaxLon).InclusiveBetween(-180, 180).When(x => x.MaxLon.HasValue);
        RuleFor(x => x).Must(x => !x.MinLat.HasValue || !x.MaxLat.HasValue || x.MinLat <= x.MaxLat)
            .WithMessage("minLat must be less than or equal to maxLat.");
        RuleFor(x => x).Must(x => !x.MinLon.HasValue || !x.MaxLon.HasValue || x.MinLon <= x.MaxLon)
            .WithMessage("minLon must be less than or equal to maxLon.");
    }
}

public sealed class SeismicEventsQueryValidator : AbstractValidator<SeismicEventsQuery>
{
    public SeismicEventsQueryValidator()
    {
        RuleFor(x => x.MinMagnitude).InclusiveBetween(-2, 10).When(x => x.MinMagnitude.HasValue);
    }
}

public sealed class NearbyEventsQueryValidator : AbstractValidator<NearbyEventsQuery>
{
    public NearbyEventsQueryValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lon).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusKm).InclusiveBetween(0.1, 1000);
    }
}
