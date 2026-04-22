using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alerto.Api.OpenApi;

public sealed class AuthExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
        if (!relativePath.Contains("auth"))
        {
            return;
        }

        if (operation.RequestBody?.Content is not null &&
            operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType.Example = BuildRequestExample(relativePath);
        }

        if (operation.Responses.TryGetValue("200", out var okResponse) &&
            okResponse.Content is not null &&
            okResponse.Content.TryGetValue("application/json", out var okMediaType))
        {
            okMediaType.Example = BuildOkResponseExample(relativePath);
        }
    }

    private static IOpenApiAny? BuildRequestExample(string relativePath)
    {
        if (relativePath.EndsWith("/login"))
        {
            return new OpenApiObject
            {
                ["username"] = new OpenApiString("admin"),
                ["password"] = new OpenApiString("AlertoAdmin123!")
            };
        }

        if (relativePath.EndsWith("/verify-2fa"))
        {
            return new OpenApiObject
            {
                ["twoFactorToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.two-factor-ticket"),
                ["code"] = new OpenApiString("123456")
            };
        }

        if (relativePath.EndsWith("/refresh") || relativePath.EndsWith("/logout"))
        {
            return new OpenApiObject
            {
                ["refreshToken"] = new OpenApiString("pGs0gFvK1oJcM8V4k0o0fCsJg4U3dK...")
            };
        }

        if (relativePath.EndsWith("/m2m/token"))
        {
            return new OpenApiObject
            {
                ["clientId"] = new OpenApiString("rules-engine"),
                ["clientSecret"] = new OpenApiString("rules-engine-secret")
            };
        }

        if (relativePath.EndsWith("/enable"))
        {
            return new OpenApiObject
            {
                ["code"] = new OpenApiString("123456")
            };
        }

        return null;
    }

    private static IOpenApiAny? BuildOkResponseExample(string relativePath)
    {
        if (relativePath.EndsWith("/2fa/setup"))
        {
            return new OpenApiObject
            {
                ["secret"] = new OpenApiString("MZXW6YTBOI======"),
                ["provisioningUri"] = new OpenApiString("otpauth://totp/Alerto%20API:admin?secret=MZXW6YTBOI======&issuer=Alerto%20API"),
                ["isEnabled"] = new OpenApiBoolean(false)
            };
        }

        if (relativePath.EndsWith("/m2m/token"))
        {
            return BuildAuthenticationResponse("rules-engine", "RulesEngine", false, false);
        }

        if (relativePath.EndsWith("/verify-2fa") || relativePath.EndsWith("/login") || relativePath.EndsWith("/refresh"))
        {
            return BuildAuthenticationResponse("admin", "Admin", false, true);
        }

        return null;
    }

    private static OpenApiObject BuildAuthenticationResponse(string username, string role, bool requiresTwoFactor, bool includeRefreshToken)
    {
        return new OpenApiObject
        {
            ["tokenType"] = new OpenApiString("Bearer"),
            ["username"] = new OpenApiString(username),
            ["role"] = new OpenApiString(role),
            ["requiresTwoFactor"] = new OpenApiBoolean(requiresTwoFactor),
            ["accessToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.access-token"),
            ["accessTokenExpiresAtUtc"] = new OpenApiString("2026-04-21T18:35:00Z"),
            ["refreshToken"] = includeRefreshToken ? new OpenApiString("pGs0gFvK1oJcM8V4k0o0fCsJg4U3dK...") : new OpenApiNull(),
            ["refreshTokenExpiresAtUtc"] = includeRefreshToken ? new OpenApiString("2026-04-28T18:20:00Z") : new OpenApiNull(),
            ["twoFactorToken"] = new OpenApiNull(),
            ["twoFactorTokenExpiresAtUtc"] = new OpenApiNull()
        };
    }
}
