using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alerto.Api.Extensions;
using Alerto.Api.HealthChecks;
using Alerto.Api.Middlewares;
using Alerto.Api.Observability;
using Alerto.Api.OpenApi;
using Alerto.Api.Security;
using Alerto.Application;
using Alerto.Infrastructure;
using Alerto.Infrastructure.Authentication;
using Alerto.Infrastructure.Persistence;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json;
using System.Threading.RateLimiting;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Fail fast if production secrets have not been overridden
if (builder.Environment.IsProduction())
{
    var secretKey = builder.Configuration["Jwt:SecretKey"];
    var dbPassword = builder.Configuration.GetConnectionString("AlertoDb");
    var adminPassword = builder.Configuration["BootstrapAdmin:Password"];

    if (string.IsNullOrWhiteSpace(secretKey) || secretKey.StartsWith("Alerto.Super"))
        throw new InvalidOperationException("PRODUCTION: Jwt:SecretKey must be set via environment variable or secrets vault.");

    if (dbPassword is null || dbPassword.Contains("Password=postgres"))
        throw new InvalidOperationException("PRODUCTION: ConnectionStrings:AlertoDb must use a strong password.");

    if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword == "AlertoAdmin123!")
        throw new InvalidOperationException("PRODUCTION: BootstrapAdmin:Password must be overridden before first deploy.");
}

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Alerto.Api")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection(ObservabilityOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.AddSingleton<IApiMetrics, ApiMetrics>();
builder.Services.AddHealthChecks()
    .AddCheck<ApiHealthCheck>("api", tags: ["live", "ready", "self"]);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

var rateLimitingOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
    ?? new RateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        var payload = JsonSerializer.Serialize(new
        {
            type = "about:blank",
            title = "Too Many Requests",
            status = 429,
            detail = "Se excedio el limite de solicitudes permitido para este cliente.",
            instance = context.HttpContext.Request.Path.Value,
            traceId = context.HttpContext.TraceIdentifier
        });

        await context.HttpContext.Response.WriteAsync(payload, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (!rateLimitingOptions.Enabled)
        {
            return RateLimitPartition.GetNoLimiter("disabled");
        }

        var isAuthEndpoint = httpContext.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase);
        var permitLimit = isAuthEndpoint ? rateLimitingOptions.AuthPermitLimit : rateLimitingOptions.PermitLimit;
        var windowSeconds = isAuthEndpoint ? rateLimitingOptions.AuthWindowSeconds : rateLimitingOptions.WindowSeconds;
        var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.Identity?.Name}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                SegmentsPerWindow = 2,
                QueueLimit = rateLimitingOptions.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("La configuración Jwt es obligatoria.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(15),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer")
                    .LogWarning(context.Exception, "JWT authentication failed. TraceId={TraceId}", context.HttpContext.TraceIdentifier);

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                var payload = JsonSerializer.Serialize(new
                {
                    type = "about:blank",
                    title = "Unauthorized",
                    status = 401,
                    detail = "Se requiere un Bearer token valido para acceder al recurso.",
                    instance = context.Request.Path.Value,
                    traceId = context.HttpContext.TraceIdentifier
                });

                await context.Response.WriteAsync(payload);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";

                var payload = JsonSerializer.Serialize(new
                {
                    type = "about:blank",
                    title = "Forbidden",
                    status = 403,
                    detail = "El usuario autenticado no tiene permisos suficientes para esta operacion.",
                    instance = context.Request.Path.Value,
                    traceId = context.HttpContext.TraceIdentifier
                });

                await context.Response.WriteAsync(payload);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Admin, policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy(AuthPolicies.Operator, policy =>
        policy.RequireRole("Admin", "Operator"));

    options.AddPolicy(AuthPolicies.Analyst, policy =>
        policy.RequireRole("Admin", "Analyst"));

    options.AddPolicy(AuthPolicies.Auditor, policy =>
        policy.RequireRole("Admin", "Auditor"));

    options.AddPolicy(AuthPolicies.AlertReaders, policy =>
        policy.RequireRole("Admin", "Operator", "Analyst", "Auditor", "RulesEngine"));

    options.AddPolicy(AuthPolicies.AlertOperators, policy =>
        policy.RequireRole("Admin", "Operator"));

    options.AddPolicy(AuthPolicies.AlertApprovers, policy =>
        policy.RequireRole("Admin", "Analyst"));

    options.AddPolicy(AuthPolicies.Dispatchers, policy =>
        policy.RequireRole("Admin", "Analyst", "RulesEngine"));

    options.AddPolicy(AuthPolicies.GeofenceReaders, policy =>
        policy.RequireRole("Admin", "Operator", "Analyst", "Auditor", "RulesEngine"));

    options.AddPolicy(AuthPolicies.GeofenceManagers, policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy(AuthPolicies.UserAdministrators, policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy(AuthPolicies.AdminsOnly, policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Alerto Management API",
        Version = "v1",
        Description = """
            API RESTful versionada para la gestion integral de alertas civiles georreferenciadas en Medellin.

            Esta API actua como Backend for Frontend para Tablero COE y Panel de Administracion, y tambien
            habilita integracion M2M con motores de reglas u otros consumidores institucionales.

            Flujo sugerido desde Swagger:
            1. Ejecutar POST /api/v1/auth/login o /api/v1/auth/m2m/token.
            2. Si aplica 2FA, completar POST /api/v1/auth/verify-2fa.
            3. Presionar "Authorize" e ingresar "Bearer {token}".
            4. Probar endpoints de Alerts, Geofences y Users con los ejemplos incluidos.
            """
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = """
            Autenticacion JWT Bearer.

            Valor esperado en Swagger:
            Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
            """
    });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.OperationFilter<AlertsExamplesOperationFilter>();
    options.OperationFilter<AdministrationExamplesOperationFilter>();
    options.OperationFilter<AuthExamplesOperationFilter>();
    options.OperationFilter<ApiDocumentationOperationFilter>();
    options.SchemaFilter<ApiSchemaDocumentationFilter>();
    options.DocumentFilter<ApiDocumentFilter>();
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = new AlertoDbInitializer(
        scope.ServiceProvider.GetRequiredService<AlertoDbContext>(),
        scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BootstrapAdminOptions>>(),
        scope.ServiceProvider.GetRequiredService<Alerto.Application.Common.Interfaces.IPasswordHasher>(),
        scope.ServiceProvider.GetRequiredService<ILogger<AlertoDbInitializer>>());

    await initializer.InitializeAsync();
}

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Alerto API v1");
        options.DocumentTitle = "Alerto Management API Docs";
        options.DefaultModelsExpandDepth(2);
        options.DisplayRequestDuration();
        options.EnablePersistAuthorization();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.Run();

public partial class Program;
