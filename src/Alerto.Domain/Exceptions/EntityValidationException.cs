namespace Alerto.Domain.Exceptions;

public sealed class EntityValidationException : DomainException
{
    public EntityValidationException(string message)
        : base(message)
    {
    }
}
