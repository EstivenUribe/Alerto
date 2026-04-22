using Alerto.Application.DTOs.Requests;
using FluentValidation;

namespace Alerto.Application.Validators;

/// <summary>
/// Validador para ApproveAlertRequest.
/// </summary>
public class ApproveAlertValidator : AbstractValidator<ApproveAlertRequest>
{
    public ApproveAlertValidator()
    {
        RuleFor(x => x.Comentario)
            .MaximumLength(500)
            .WithMessage("El comentario no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Comentario));
    }
}
