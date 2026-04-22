namespace Alerto.Domain.Exceptions;

public sealed class ApprovalWindowExpiredException : DomainException
{
    public ApprovalWindowExpiredException()
        : base("La alerta excedio el timeout maximo de aprobacion de 3 minutos.")
    {
    }
}
