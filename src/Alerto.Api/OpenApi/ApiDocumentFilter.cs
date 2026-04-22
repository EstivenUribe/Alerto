using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alerto.Api.OpenApi;

public sealed class ApiDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags =
        [
            new OpenApiTag
            {
                Name = "Authentication",
                Description = "Autenticacion humana y M2M, JWT, refresh tokens y 2FA TOTP."
            },
            new OpenApiTag
            {
                Name = "Alerts",
                Description = "Gestion del ciclo de vida de alertas civiles georreferenciadas."
            },
            new OpenApiTag
            {
                Name = "Geofences",
                Description = "Administracion de geocercas operativas para clasificacion y filtrado territorial."
            },
            new OpenApiTag
            {
                Name = "Users",
                Description = "Administracion de usuarios, roles operativos y estado de acceso."
            },
            new OpenApiTag
            {
                Name = "Observability",
                Description = "Metricas y endpoints de soporte para operacion y evaluacion tecnica."
            }
        ];
    }
}
