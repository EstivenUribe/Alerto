using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Alerto.Infrastructure.Auth;

/// <summary>
/// Genera tokens JWT (access + refresh) para el flujo de autenticación.
/// Implementa el diseño preliminar JWT del sistema Alerto.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;

        var secretKey = config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey no configurada");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Genera un access token JWT con claims del usuario.
    /// Expiración: 15 minutos.
    /// </summary>
    public string GenerarAccessToken(int usuarioId, string email, string nombre, string rol)
    {
        var claims = new List<Claim>
        {
            new("sub", usuarioId.ToString()),
            new("email", email),
            new("name", nombre),
            new("role", rol),
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expMinutes = int.Parse(_config["Jwt:AccessTokenExpirationMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: _signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un token temporal para el flujo 2FA.
    /// Expiración: 5 minutos. Solo contiene sub + email.
    /// </summary>
    public string GenerarTempToken(int usuarioId, string email)
    {
        var claims = new List<Claim>
        {
            new("sub", usuarioId.ToString()),
            new("email", email),
            new("purpose", "2fa-verification"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expMinutes = int.Parse(_config["Jwt:TempTokenExpirationMinutes"] ?? "5");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: _signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un refresh token opaco (no JWT) para renovación.
    /// Se almacena hasheado en BD. Expiración: 7 días.
    /// </summary>
    public RefreshTokenResult GenerarRefreshToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes);
        var hash = ComputeSha256Hash(token);

        var expDays = int.Parse(_config["Jwt:RefreshTokenExpirationDays"] ?? "7");

        return new RefreshTokenResult
        {
            Token = token,           // Se envía al cliente
            TokenHash = hash,        // Se guarda en BD
            ExpiresAt = DateTime.UtcNow.AddDays(expDays)
        };
    }

    /// <summary>
    /// Valida un refresh token comparando su hash con el almacenado.
    /// </summary>
    public bool ValidarRefreshToken(string token, string storedHash)
    {
        var hash = ComputeSha256Hash(token);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// ── Modelos auxiliares ──

public record RefreshTokenResult
{
    public string Token { get; init; } = default!;
    public string TokenHash { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Interfaz para el generador de tokens JWT.
/// Definida en Domain, implementada en Infrastructure.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerarAccessToken(int usuarioId, string email, string nombre, string rol);
    string GenerarTempToken(int usuarioId, string email);
    RefreshTokenResult GenerarRefreshToken();
    bool ValidarRefreshToken(string token, string storedHash);
}
