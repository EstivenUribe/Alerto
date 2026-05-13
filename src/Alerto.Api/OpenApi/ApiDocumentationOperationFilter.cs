using Alerto.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alerto.Api.OpenApi;

public sealed class ApiDocumentationOperationFilter : IOperationFilter
{
    private static readonly Dictionary<string, string> PolicyDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        [AuthPolicies.Admin] = "Roles permitidos: Admin.",
        [AuthPolicies.Operator] = "Roles permitidos: Admin, Operator.",
        [AuthPolicies.Analyst] = "Roles permitidos: Admin, Analyst.",
        [AuthPolicies.Auditor] = "Roles permitidos: Admin, Auditor.",
        [AuthPolicies.AlertReaders] = "Roles permitidos: Admin, Operator, Analyst, Auditor, RulesEngine, Citizen.",
        [AuthPolicies.AlertOperators] = "Roles permitidos: Admin, Operator.",
        [AuthPolicies.AlertCreators] = "Roles permitidos: Admin, Operator, Citizen.",
        [AuthPolicies.AlertApprovers] = "Roles permitidos: Admin, Operator, Analyst.",
        [AuthPolicies.CitizenConfirmers] = "Roles permitidos: Admin, Operator, Citizen.",
        [AuthPolicies.ConfirmationReaders] = "Roles permitidos: Admin, Operator, Analyst.",
        [AuthPolicies.Dispatchers] = "Roles permitidos: Admin, Analyst, RulesEngine.",
        [AuthPolicies.GeofenceReaders] = "Roles permitidos: Admin, Operator, Analyst, Auditor, RulesEngine, Citizen.",
        [AuthPolicies.GeofenceManagers] = "Roles permitidos: Admin.",
        [AuthPolicies.UserAdministrators] = "Roles permitidos: Admin.",
        [AuthPolicies.AdminsOnly] = "Roles permitidos: Admin."
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Tags = [new OpenApiTag { Name = ResolveModule(context.ApiDescription.RelativePath) }];

        var authorizationText = BuildAuthorizationDescription(context);
        if (!string.IsNullOrWhiteSpace(authorizationText))
        {
            operation.Description = string.IsNullOrWhiteSpace(operation.Description)
                ? authorizationText
                : $"{operation.Description}\n\n{authorizationText}";
        }

        if (RequiresAuthentication(context))
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        }
                    ] = Array.Empty<string>()
                }
            ];
        }

        EnrichProblemResponses(operation);
    }

    private static string ResolveModule(string? relativePath)
    {
        var path = relativePath?.ToLowerInvariant() ?? string.Empty;
        if (path.Contains("/auth")) return "Authentication";
        if (path.Contains("/alerts")) return "Alerts";
        if (path.Contains("/weather")) return "Weather";
        if (path.Contains("/geofences")) return "Geofences";
        if (path.Contains("/users")) return "Users";
        if (path.Contains("/metrics")) return "Observability";
        return "General";
    }

    private static string BuildAuthorizationDescription(OperationFilterContext context)
    {
        if (!RequiresAuthentication(context))
        {
            return "Endpoint publico. No requiere token JWT.";
        }

        var authorizeAttributes = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>() ?? [])
            .ToArray();

        var policies = authorizeAttributes
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (policies.Length == 0)
        {
            return "Endpoint protegido. Requiere JWT Bearer valido.";
        }

        var policyDetails = policies
            .Select(policy => PolicyDescriptions.TryGetValue(policy!, out var description)
                ? $"{policy}: {description}"
                : policy)
            .ToArray();

        return $"Endpoint protegido. Requiere JWT Bearer valido.\n\nAutorizacion:\n- {string.Join("\n- ", policyDetails)}";
    }

    private static bool RequiresAuthentication(OperationFilterContext context)
    {
        var method = context.MethodInfo;
        if (method.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any())
        {
            return false;
        }

        if (method.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true)
        {
            return false;
        }

        return method.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
               method.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true;
    }

    private static void EnrichProblemResponses(OpenApiOperation operation)
    {
        foreach (var (statusCode, response) in operation.Responses)
        {
            if (statusCode is not ("400" or "401" or "403" or "404" or "409" or "422" or "429" or "502" or "503"))
            {
                continue;
            }

            response.Description = statusCode switch
            {
                "400" => "Solicitud invalida o error de validacion.",
                "401" => "Autenticacion requerida o credenciales invalidas.",
                "403" => "El usuario autenticado no tiene permisos suficientes.",
                "404" => "Recurso no encontrado.",
                "409" => "Conflicto de concurrencia o estado del recurso.",
                "422" => "Regla de negocio violada.",
                "429" => "Se excedio el limite de solicitudes.",
                "502" => "Error al consultar una dependencia externa.",
                "503" => "Dependencia externa no disponible.",
                _ => response.Description
            };

            response.Content ??= new Dictionary<string, OpenApiMediaType>();
            response.Content["application/problem+json"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = statusCode == "400" ? nameof(ValidationProblemDetails) : nameof(ProblemDetails)
                    }
                },
                Example = statusCode == "400"
                    ? BuildValidationProblemExample()
                    : BuildProblemExample(statusCode)
            };
        }
    }

    private static IOpenApiAny BuildProblemExample(string statusCode)
    {
        return new OpenApiObject
        {
            ["type"] = new OpenApiString("about:blank"),
            ["title"] = new OpenApiString(statusCode switch
            {
                "401" => "Unauthorized",
                "403" => "Forbidden",
                "404" => "Resource not found",
                "409" => "Concurrency conflict",
                "422" => "Business rule violation",
                "429" => "Too Many Requests",
                "502" => "Bad Gateway",
                "503" => "External dependency unavailable",
                _ => "Unexpected error"
            }),
            ["status"] = new OpenApiInteger(int.Parse(statusCode)),
            ["detail"] = new OpenApiString(statusCode switch
            {
                "401" => "Se requiere un Bearer token valido para acceder al recurso.",
                "403" => "El usuario autenticado no tiene permisos suficientes para esta operacion.",
                "404" => "No existe el recurso solicitado.",
                "409" => "El recurso fue modificado por otro proceso. Recargue y reintente.",
                "422" => "La transicion solicitada viola una regla del dominio.",
                "429" => "Se excedio el limite de solicitudes permitido para este cliente.",
                "502" => "No fue posible completar la consulta hacia una dependencia externa.",
                "503" => "Una dependencia externa requerida no se encuentra disponible.",
                _ => "Se produjo un error inesperado."
            }),
            ["instance"] = new OpenApiString("/api/v1/alerts"),
            ["traceId"] = new OpenApiString("0HN9J4I2Q0LAA:00000001")
        };
    }

    private static IOpenApiAny BuildValidationProblemExample()
    {
        return new OpenApiObject
        {
            ["type"] = new OpenApiString("about:blank"),
            ["title"] = new OpenApiString("Validation failed"),
            ["status"] = new OpenApiInteger(400),
            ["detail"] = new OpenApiString("Uno o mas errores de validacion impiden procesar la solicitud."),
            ["instance"] = new OpenApiString("/api/v1/auth/login"),
            ["traceId"] = new OpenApiString("0HN9J4I2Q0LAA:00000002"),
            ["errors"] = new OpenApiObject
            {
                ["username"] = new OpenApiArray { new OpenApiString("'Username' must not be empty.") },
                ["password"] = new OpenApiArray { new OpenApiString("'Password' must not be empty.") }
            }
        };
    }
}
