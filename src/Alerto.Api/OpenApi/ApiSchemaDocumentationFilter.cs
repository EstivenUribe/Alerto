using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alerto.Api.OpenApi;

public sealed class ApiSchemaDocumentationFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))
        {
            schema.Description = "Estructura estandar RFC 7807 para reportar errores de la API.";
            return;
        }

        if (context.Type == typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))
        {
            schema.Description = "Variante RFC 7807 con detalle de errores de validacion por campo.";
            return;
        }

        if (context.Type.IsEnum)
        {
            var values = Enum.GetNames(context.Type);
            schema.Description = string.IsNullOrWhiteSpace(schema.Description)
                ? $"Valores permitidos: {string.Join(", ", values)}."
                : $"{schema.Description} Valores permitidos: {string.Join(", ", values)}.";
            schema.Example = new OpenApiString(values.First());
        }
    }
}
