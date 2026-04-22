using Alerto.Application.DTOs.Requests;
using FluentValidation;

namespace Alerto.Application.Validators;

/// <summary>
/// Validador para CreateAlertRequest.
/// </summary>
public class CreateAlertValidator : AbstractValidator<CreateAlertRequest>
{
    public CreateAlertValidator()
    {
        RuleFor(x => x.IdentificadorCap)
            .NotEmpty()
            .WithMessage("El identificador CAP es requerido")
            .MinimumLength(10)
            .WithMessage("El identificador CAP debe tener al menos 10 caracteres")
            .MaximumLength(255)
            .WithMessage("El identificador CAP no puede exceder 255 caracteres")
            .Matches(@"^urn:oid:\d+(\.\d+)*$")
            .WithMessage("El identificador CAP debe tener formato válido (ej: urn:oid:2.49.0.0.170.0.2026...)");

        RuleFor(x => x.Severidad)
            .IsInEnum()
            .WithMessage("La severidad debe ser un valor válido (Advisory, Watch, Warning, Emergency)");

        RuleFor(x => x.Evento)
            .NotEmpty()
            .WithMessage("El evento es requerido")
            .MinimumLength(5)
            .WithMessage("El evento debe tener al menos 5 caracteres")
            .MaximumLength(255)
            .WithMessage("El evento no puede exceder 255 caracteres");

        RuleFor(x => x.GeocercaId)
            .GreaterThan(0)
            .WithMessage("El ID de geocerca debe ser mayor que 0");

        RuleFor(x => x.MensajeEs)
            .NotEmpty()
            .WithMessage("El mensaje en español es requerido")
            .MinimumLength(10)
            .WithMessage("El mensaje debe tener al menos 10 caracteres")
            .MaximumLength(1000)
            .WithMessage("El mensaje no puede exceder 1000 caracteres");

        RuleFor(x => x.PoblacionObjetivo)
            .GreaterThan(0)
            .WithMessage("La población objetivo debe ser mayor que 0");
    }
}
