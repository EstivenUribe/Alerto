using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alerto.Api.OpenApi;

public sealed class AdministrationExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
        if (relativePath.Contains("geofences"))
        {
            ApplyGeofenceExamples(operation, relativePath);
            return;
        }

        if (relativePath.Contains("users"))
        {
            ApplyUserExamples(operation, relativePath);
        }
    }

    private static void ApplyGeofenceExamples(OpenApiOperation operation, string relativePath)
    {
        foreach (var parameter in operation.Parameters)
        {
            switch (parameter.Name)
            {
                case "search":
                    parameter.Example = new OpenApiString("sur oriental");
                    break;
                case "neighborhood":
                    parameter.Example = new OpenApiString("El Poblado");
                    break;
                case "isActive":
                    parameter.Example = new OpenApiBoolean(true);
                    break;
                case "pageNumber":
                    parameter.Example = new OpenApiInteger(1);
                    break;
                case "pageSize":
                    parameter.Example = new OpenApiInteger(20);
                    break;
            }
        }

        if (operation.RequestBody?.Content is not null &&
            operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            if (relativePath.EndsWith("/geofences"))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["code"] = new OpenApiString("MEDE-SUR-01"),
                    ["name"] = new OpenApiString("Corredor Rio Medellin Sur"),
                    ["polygonWkt"] = new OpenApiString("POLYGON((-75.58 6.21,-75.56 6.21,-75.56 6.23,-75.58 6.23,-75.58 6.21))"),
                    ["neighborhood"] = new OpenApiString("Guayabal")
                };
            }
            else if (relativePath.EndsWith("/activate"))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["expectedVersion"] = new OpenApiInteger(3),
                    ["reason"] = new OpenApiString("La zona vuelve a estar habilitada para monitoreo operativo.")
                };
            }
            else if (relativePath.EndsWith("/deactivate"))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["expectedVersion"] = new OpenApiInteger(3),
                    ["reason"] = new OpenApiString("La delimitacion requiere ajuste cartografico antes de nuevas alertas.")
                };
            }
            else
            {
                mediaType.Example = new OpenApiObject
                {
                    ["name"] = new OpenApiString("Corredor Rio Medellin Sur Ajustado"),
                    ["polygonWkt"] = new OpenApiString("POLYGON((-75.581 6.211,-75.559 6.211,-75.559 6.231,-75.581 6.231,-75.581 6.211))"),
                    ["neighborhood"] = new OpenApiString("Guayabal"),
                    ["expectedVersion"] = new OpenApiInteger(3)
                };
            }
        }

        if (operation.Responses.TryGetValue("200", out var okResponse) &&
            okResponse.Content is not null &&
            okResponse.Content.TryGetValue("application/json", out var okMediaType))
        {
            okMediaType.Example = operation.OperationId?.Contains("Search", StringComparison.OrdinalIgnoreCase) == true
                ? BuildGeofenceListResponse()
                : BuildGeofenceResponse();
        }
    }

    private static void ApplyUserExamples(OpenApiOperation operation, string relativePath)
    {
        foreach (var parameter in operation.Parameters)
        {
            switch (parameter.Name)
            {
                case "search":
                    parameter.Example = new OpenApiString("coordinador");
                    break;
                case "role":
                    parameter.Example = new OpenApiString("Operator");
                    break;
                case "isActive":
                    parameter.Example = new OpenApiBoolean(true);
                    break;
                case "pageNumber":
                    parameter.Example = new OpenApiInteger(1);
                    break;
                case "pageSize":
                    parameter.Example = new OpenApiInteger(20);
                    break;
            }
        }

        if (operation.RequestBody?.Content is not null &&
            operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            if (relativePath.EndsWith("/users"))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["username"] = new OpenApiString("operador.norte"),
                    ["displayName"] = new OpenApiString("Operador Norte"),
                    ["email"] = new OpenApiString("operador.norte@alerto.local"),
                    ["password"] = new OpenApiString("OperadorNorte2026!"),
                    ["role"] = new OpenApiString("Operator")
                };
            }
            else if (relativePath.EndsWith("/activate"))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["expectedVersion"] = new OpenApiInteger(4),
                    ["reason"] = new OpenApiString("Usuario rehabilitado por retorno al turno operativo.")
                };
            }
            else if (relativePath.EndsWith("/deactivate"))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["expectedVersion"] = new OpenApiInteger(4),
                    ["reason"] = new OpenApiString("Suspension administrativa del acceso al panel.")
                };
            }
            else
            {
                mediaType.Example = new OpenApiObject
                {
                    ["displayName"] = new OpenApiString("Operador Norte Senior"),
                    ["email"] = new OpenApiString("operador.norte@alerto.local"),
                    ["role"] = new OpenApiString("Analyst"),
                    ["expectedVersion"] = new OpenApiInteger(4)
                };
            }
        }

        if (operation.Responses.TryGetValue("200", out var okResponse) &&
            okResponse.Content is not null &&
            okResponse.Content.TryGetValue("application/json", out var okMediaType))
        {
            okMediaType.Example = operation.OperationId?.Contains("Search", StringComparison.OrdinalIgnoreCase) == true
                ? BuildUserListResponse()
                : BuildUserResponse();
        }
    }

    private static IOpenApiAny BuildGeofenceListResponse()
    {
        return new OpenApiObject
        {
            ["items"] = new OpenApiArray { BuildGeofenceResponse() },
            ["pageNumber"] = new OpenApiInteger(1),
            ["pageSize"] = new OpenApiInteger(20),
            ["totalCount"] = new OpenApiInteger(1),
            ["totalPages"] = new OpenApiInteger(1),
            ["hasPreviousPage"] = new OpenApiBoolean(false),
            ["hasNextPage"] = new OpenApiBoolean(false)
        };
    }

    private static IOpenApiAny BuildUserListResponse()
    {
        return new OpenApiObject
        {
            ["items"] = new OpenApiArray { BuildUserResponse() },
            ["pageNumber"] = new OpenApiInteger(1),
            ["pageSize"] = new OpenApiInteger(20),
            ["totalCount"] = new OpenApiInteger(1),
            ["totalPages"] = new OpenApiInteger(1),
            ["hasPreviousPage"] = new OpenApiBoolean(false),
            ["hasNextPage"] = new OpenApiBoolean(false)
        };
    }

    private static OpenApiObject BuildGeofenceResponse()
    {
        return new OpenApiObject
        {
            ["id"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
            ["code"] = new OpenApiString("MEDE-SUR-01"),
            ["name"] = new OpenApiString("Corredor Rio Medellin Sur"),
            ["polygonWkt"] = new OpenApiString("POLYGON((-75.58 6.21,-75.56 6.21,-75.56 6.23,-75.58 6.23,-75.58 6.21))"),
            ["neighborhood"] = new OpenApiString("Guayabal"),
            ["isActive"] = new OpenApiBoolean(true),
            ["createdAtUtc"] = new OpenApiString("2026-04-21T14:00:00Z"),
            ["updatedAtUtc"] = new OpenApiString("2026-04-21T14:10:00Z"),
            ["version"] = new OpenApiInteger(3)
        };
    }

    private static OpenApiObject BuildUserResponse()
    {
        return new OpenApiObject
        {
            ["id"] = new OpenApiString("22222222-2222-2222-2222-222222222222"),
            ["username"] = new OpenApiString("operador.norte"),
            ["displayName"] = new OpenApiString("Operador Norte"),
            ["email"] = new OpenApiString("operador.norte@alerto.local"),
            ["role"] = new OpenApiString("Operator"),
            ["isActive"] = new OpenApiBoolean(true),
            ["isTwoFactorEnabled"] = new OpenApiBoolean(true),
            ["createdAtUtc"] = new OpenApiString("2026-04-20T18:00:00Z"),
            ["updatedAtUtc"] = new OpenApiString("2026-04-21T09:15:00Z"),
            ["version"] = new OpenApiInteger(4)
        };
    }
}
