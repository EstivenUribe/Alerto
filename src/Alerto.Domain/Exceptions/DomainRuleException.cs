namespace Alerto.Domain.Exceptions;

public sealed class DomainRuleException : DomainException
{
    public DomainRuleException(string message)
        : base(message)
    {
    }
}
