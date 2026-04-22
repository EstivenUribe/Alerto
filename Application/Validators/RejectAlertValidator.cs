using Alerto.Application.DTOs.Requests;
using FluentValidation;

namespace Alerto.Application.Validators;

/// <summary>
/// Validador para RejectAlertRequest.
/// </summary>
public class RejectAlertValidator : AbstractValidator<RejectAlertRequest>
{
    public RejectAlertValidator()
    {
        RuleFor(x => x.Justificacion)
            .NotEmpty()
            .WithMessage("La justificación del rechazo es requerida")
            .MinimumLength(10)
            .WithMessage("La justificación debe tener al menos 10 caracteres")
            .MaximumLength(500)
            .WithMessage("La justificación no puede exceder 500 caracteres");
    }
}
