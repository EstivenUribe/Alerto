using Alerto.Application.Common.Interfaces;

namespace Alerto.Infrastructure.Authentication;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _passwordHasher = new();
    private static readonly object User = new();

    public string HashPassword(string password) => _passwordHasher.HashPassword(User, password);

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(User, hashedPassword, providedPassword);
        return result is Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success
            or Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded;
    }
}
