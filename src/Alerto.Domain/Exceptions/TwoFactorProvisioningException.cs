namespace Alerto.Domain.Exceptions;

public sealed class TwoFactorProvisioningException : DomainException
{
    public TwoFactorProvisioningException(string message)
        : base(message)
    {
    }
}
