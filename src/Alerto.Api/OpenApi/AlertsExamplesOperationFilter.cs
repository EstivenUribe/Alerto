using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alerto.Api.OpenApi;

public sealed class AlertsExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
        if (!relativePath.Contains("alerts"))
        {
            return;
        }

        EnrichQueryExamples(operation);
        EnrichRequestExamples(operation, relativePath);
        EnrichResponseExamples(operation, relativePath);
    }

    private static void EnrichQueryExamples(OpenApiOperation operation)
    {
        foreach (var parameter in operation.Parameters)
        {
            switch (parameter.Name)
            {
                case "status":
                    parameter.Example = new OpenApiString("Pending");
                    break;
                case "geofenceId":
                    parameter.Example = new OpenApiString("11111111-1111-1111-1111-111111111111");
                    break;
                case "severity":
                    parameter.Example = new OpenApiString("Critical");
                    break;
                case "createdFromUtc":
                    parameter.Example = new OpenApiString("2026-04-21T00:00:00Z");
                    break;
                case "createdToUtc":
                    parameter.Example = new OpenApiString("2026-04-21T23:59:59Z");
                    break;
                case "pageNumber":
                    parameter.Example = new OpenApiInteger(1);
                    break;
                case "pageSize":
                    parameter.Example = new OpenApiInteger(20);
                    break;
            }
        }
    }

    private static void EnrichRequestExamples(OpenApiOperation operation, string relativePath)
    {
        if (operation.RequestBody?.Content is null || !operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            return;
        }

        if (relativePath.EndsWith("/alerts"))
        {
            mediaType.Example = new OpenApiObject
            {
                ["title"] = new OpenApiString("Creciente súbita río Medellín"),
                ["description"] = new OpenApiString("Se detecta aumento acelerado del caudal con riesgo para sectores ribereños."),
                ["severity"] = new OpenApiString("Critical"),
                ["sourceSystem"] = new OpenApiString("Tablero COE"),
                ["address"] = new OpenApiString("Av. Regional con Calle 30, Medellín"),
                ["latitude"] = new OpenApiDouble(6.230145),
                ["longitude"] = new OpenApiDouble(-75.573921),
                ["geofenceId"] = new OpenApiString("11111111-1111-1111-1111-111111111111")
            };
        }

        if (relativePath.EndsWith("/approve"))
        {
            mediaType.Example = new OpenApiObject
            {
                ["expectedVersion"] = new OpenApiInteger(0)
            };
        }

        if (relativePath.EndsWith("/reject") || relativePath.EndsWith("/cancel"))
        {
            mediaType.Example = new OpenApiObject
            {
                ["expectedVersion"] = new OpenApiInteger(0),
                ["reason"] = new OpenApiString("Validación operativa desfavorable por inconsistencia en la fuente.")
            };
        }
    }

    private static void EnrichResponseExamples(OpenApiOperation operation, string relativePath)
    {
        if (!operation.Responses.TryGetValue("200", out var okResponse) ||
            okResponse.Content is null ||
            !okResponse.Content.TryGetValue("application/json", out var okMediaType))
        {
            return;
        }

        if (relativePath.EndsWith("/alerts") && operation.OperationId?.Contains("Search", StringComparison.OrdinalIgnoreCase) == true)
        {
            okMediaType.Example = BuildListResponse();
            return;
        }

        okMediaType.Example = BuildSingleAlertResponse();
    }

    private static IOpenApiAny BuildListResponse()
    {
        return new OpenApiObject
        {
            ["items"] = new OpenApiArray { BuildSingleAlertResponse() },
            ["pageNumber"] = new OpenApiInteger(1),
            ["pageSize"] = new OpenApiInteger(20),
            ["totalCount"] = new OpenApiInteger(1),
            ["totalPages"] = new OpenApiInteger(1),
            ["hasPreviousPage"] = new OpenApiBoolean(false),
            ["hasNextPage"] = new OpenApiBoolean(false)
        };
    }

    private static OpenApiObject BuildSingleAlertResponse()
    {
        return new OpenApiObject
        {
            ["id"] = new OpenApiString("7d1af4cd-31d9-4a88-8ac2-b0b0cc2f9d44"),
            ["title"] = new OpenApiString("Creciente súbita río Medellín"),
            ["description"] = new OpenApiString("Se detecta aumento acelerado del caudal con riesgo para sectores ribereños."),
            ["severity"] = new OpenApiString("Critical"),
            ["status"] = new OpenApiString("Pending"),
            ["sourceSystem"] = new OpenApiString("Tablero COE"),
            ["address"] = new OpenApiString("Av. Regional con Calle 30, Medellín"),
            ["latitude"] = new OpenApiDouble(6.230145),
            ["longitude"] = new OpenApiDouble(-75.573921),
            ["geofenceId"] = new OpenApiString("11111111-1111-1111-1111-111111111111"),
            ["createdByUserId"] = new OpenApiString("22222222-2222-2222-2222-222222222222"),
            ["approvedByUserId"] = new OpenApiNull(),
            ["createdAtUtc"] = new OpenApiString("2026-04-21T17:10:00Z"),
            ["updatedAtUtc"] = new OpenApiString("2026-04-21T17:10:00Z"),
            ["approvalDeadlineUtc"] = new OpenApiString("2026-04-21T17:13:00Z"),
            ["version"] = new OpenApiInteger(0),
            ["dispatches"] = new OpenApiArray()
        };
    }
}
