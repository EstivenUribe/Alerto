using FluentValidation;

namespace Alerto.Application.Alerts;

public sealed record AlertServiceValidators(
    IValidator<CreateAlertRequest> Create,
    IValidator<UpdateAlertRequest> Update,
    IValidator<ApproveAlertRequest> Approve,
    IValidator<RejectAlertRequest> Reject,
    IValidator<CancelAlertRequest> Cancel,
    IValidator<DeleteAlertRequest> Delete,
    IValidator<DispatchAlertRequest> Dispatch,
    IValidator<AlertQueryRequest> Query,
    IValidator<CitizenConfirmAlertRequest> CitizenConfirm);

public sealed class CreateAlertRequestValidator : AbstractValidator<CreateAlertRequest>
{
    public CreateAlertRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GeofenceId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(6.10m, 6.40m);
        RuleFor(x => x.Longitude).InclusiveBetween(-75.70m, -75.45m);
    }
}

public sealed class UpdateAlertRequestValidator : AbstractValidator<UpdateAlertRequest>
{
    public UpdateAlertRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GeofenceId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(6.10m, 6.40m);
        RuleFor(x => x.Longitude).InclusiveBetween(-75.70m, -75.45m);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}

public sealed class ApproveAlertRequestValidator : AbstractValidator<ApproveAlertRequest>
{
    public ApproveAlertRequestValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}

public sealed class RejectAlertRequestValidator : AbstractValidator<RejectAlertRequest>
{
    public RejectAlertRequestValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class CancelAlertRequestValidator : AbstractValidator<CancelAlertRequest>
{
    public CancelAlertRequestValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class DeleteAlertRequestValidator : AbstractValidator<DeleteAlertRequest>
{
    public DeleteAlertRequestValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class DispatchAlertRequestValidator : AbstractValidator<DispatchAlertRequest>
{
    public DispatchAlertRequestValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ProviderReference).NotEmpty().MaximumLength(160);
    }
}

public sealed class AlertQueryRequestValidator : AbstractValidator<AlertQueryRequest>
{
    public AlertQueryRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x)
            .Must(x => !x.CreatedFromUtc.HasValue || !x.CreatedToUtc.HasValue || x.CreatedFromUtc <= x.CreatedToUtc)
            .WithMessage("La fecha inicial debe ser menor o igual a la fecha final.");
    }
}

public sealed class CitizenConfirmAlertRequestValidator : AbstractValidator<CitizenConfirmAlertRequest>
{
    public CitizenConfirmAlertRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);
    }
}
