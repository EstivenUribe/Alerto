using FluentValidation;

namespace Alerto.Application.Geofences;

public sealed class CreateGeofenceRequestValidator : AbstractValidator<CreateGeofenceRequest>
{
    public CreateGeofenceRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PolygonWkt).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Neighborhood).NotEmpty().MaximumLength(120);
    }
}

public sealed class UpdateGeofenceRequestValidator : AbstractValidator<UpdateGeofenceRequest>
{
    public UpdateGeofenceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PolygonWkt).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Neighborhood).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}

public sealed class GeofenceQueryRequestValidator : AbstractValidator<GeofenceQueryRequest>
{
    public GeofenceQueryRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(120).When(x => !string.IsNullOrWhiteSpace(x.Search));
        RuleFor(x => x.Neighborhood).MaximumLength(120).When(x => !string.IsNullOrWhiteSpace(x.Neighborhood));
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ChangeGeofenceStatusRequestValidator : AbstractValidator<ChangeGeofenceStatusRequest>
{
    public ChangeGeofenceStatusRequestValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
