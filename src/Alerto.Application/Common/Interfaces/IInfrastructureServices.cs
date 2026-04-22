using Alerto.Application.Common.Models;

namespace Alerto.Application.Common.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}

public interface ITotpService
{
    string GenerateSecret();
    string BuildProvisioningUri(string issuer, string username, string secret);
    bool ValidateCode(string secret, string code);
}

public interface IJwtTokenService
{
    TokenEnvelope CreateUserToken(Guid userId, string username, string role, string authenticationMethod);
    TokenEnvelope CreateMachineToken(string clientId, string role, string scope);
    TokenEnvelope CreateTwoFactorToken(Guid userId, string username, string role);
    TwoFactorTicketPayload? ValidateTwoFactorToken(string token);
    string GenerateRefreshToken();
}

public interface IMachineClientValidator
{
    bool IsValid(string clientId, string clientSecret, out MachineClientDefinition? clientDefinition);
}

public interface IAppCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
