using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Alerto.Infrastructure.Authentication;

public sealed class JwtTokenService : IJwtTokenService
{
    private const string TwoFactorTokenUse = "mfa_pending";
    private readonly JwtOptions _options;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(15),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }

    public TokenEnvelope CreateUserToken(Guid userId, string username, string role, string authenticationMethod)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("amr", authenticationMethod)
        };

        return CreateToken(claims, _options.AccessTokenMinutes);
    }

    public TokenEnvelope CreateMachineToken(string clientId, string role, string scope)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clientId),
            new(ClaimTypes.Name, clientId),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("client_id", clientId),
            new("scope", scope),
            new("amr", "client_credentials")
        };

        return CreateToken(claims, _options.MachineTokenMinutes);
    }

    public TokenEnvelope CreateTwoFactorToken(Guid userId, string username, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("token_use", TwoFactorTokenUse),
            new("amr", "pwd")
        };

        return CreateToken(claims, _options.TwoFactorTokenMinutes);
    }

    public TwoFactorTicketPayload? ValidateTwoFactorToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, _validationParameters, out _);
            var tokenUse = principal.FindFirstValue("token_use");
            if (!string.Equals(tokenUse, TwoFactorTokenUse, StringComparison.Ordinal))
            {
                return null;
            }

            var userIdRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var username = principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
            var role = principal.FindFirstValue(ClaimTypes.Role);

            if (!Guid.TryParse(userIdRaw, out var userId) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            return new TwoFactorTicketPayload(userId, username, role);
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private TokenEnvelope CreateToken(IEnumerable<Claim> claims, int expiresInMinutes)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresInMinutes);
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
                SecurityAlgorithms.HmacSha256));

        return new TokenEnvelope(new JwtSecurityTokenHandler().WriteToken(tokenDescriptor), expiresAtUtc);
    }
}
