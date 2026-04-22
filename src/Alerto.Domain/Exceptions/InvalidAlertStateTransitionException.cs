using Alerto.Domain.Enums;

namespace Alerto.Domain.Exceptions;

public sealed class InvalidAlertStateTransitionException : DomainException
{
    public InvalidAlertStateTransitionException(AlertStatus currentStatus, string attemptedAction)
        : base($"La alerta en estado '{currentStatus}' no permite la accion '{attemptedAction}'.")
    {
    }
}
