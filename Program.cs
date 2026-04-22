using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Alerto.Infrastructure.Persistence;
using Alerto.Domain.Entities;
using Alerto.Application.Services;
using Alerto.Application.Validators;
using Alerto.Infrastructure.Repositories;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════
// 1. CONFIGURACIÓN JWT
// ══════════════════════════════════════════════════════════
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validar firma con clave simétrica HMAC-SHA256
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),

            // Validar emisor e audiencia
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],   // "alerto-api"

            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"], // "alerto-clients"

            // Validar expiración (access_token: 15 min)
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            // Mapeo de claims
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        // Eventos para logging de autenticación
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext
                    .RequestServices.GetRequiredService<ILogger<Program>>();

                logger.LogWarning(
                    "Autenticación fallida: {Error}", context.Exception.Message);

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext
                    .RequestServices.GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst("sub")?.Value;

                logger.LogInformation(
                    "Token validado para usuario {UserId}", userId);

                return Task.CompletedTask;
            }
        };
    });

// Políticas de autorización por rol
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOperador", policy =>
        policy.RequireRole("Operador", "Administrador"));

    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Administrador"));

    options.AddPolicy("RequireAuditor", policy =>
        policy.RequireRole("Auditor", "Administrador"));
});

// ══════════════════════════════════════════════════════════
// 2. VERSIONAMIENTO DE API (por ruta URI)
// ══════════════════════════════════════════════════════════
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ══════════════════════════════════════════════════════════
// 3. SWAGGER / OPENAPI
// ══════════════════════════════════════════════════════════
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Alerto Management API",
        Version = "v1",
        Description = "API RESTful para gestión integral de alertas civiles " +
                      "georreferenciadas en Medellín (Alerto)",
        Contact = new OpenApiContact
        {
            Name = "Equipo Alerto - Politécnico JIC"
        }
    });

    // Esquema de seguridad JWT en Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Incluir XML docs
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ══════════════════════════════════════════════════════════
// 4. BASE DE DATOS (PostgreSQL + EF Core)
// ══════════════════════════════════════════════════════════
builder.Services.AddDbContext<AlertoDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("AlertoDb"),
        npgsql => npgsql.UseNetTopologySuite() // PostGIS para geometrías
    ));

// ══════════════════════════════════════════════════════════
// 5. INYECCIÓN DE DEPENDENCIAS
// ══════════════════════════════════════════════════════════
// Infrastructure - UnitOfWork & Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

// Application Services
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

// AutoMapper - configure manually
var mapperConfig = new AutoMapper.MapperConfiguration(cfg =>
{
    cfg.AddProfile<Alerto.Application.Profiles.AlertProfile>();
    cfg.AddProfile<Alerto.Application.Profiles.ApprovalProfile>();
});
builder.Services.AddSingleton(mapperConfig.CreateMapper());

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();

var assembly = typeof(CreateAlertValidator).Assembly;
var validatorTypes = assembly.GetTypes()
    .Where(t => t.Name.EndsWith("Validator") && !t.IsAbstract && !t.IsInterface);

foreach (var validatorType in validatorTypes)
{
    var interfaces = validatorType.GetInterfaces();
    foreach (var interfaceType in interfaces)
    {
        if (interfaceType.IsGenericType &&
            interfaceType.GetGenericTypeDefinition() == typeof(FluentValidation.IValidator<>))
        {
            builder.Services.AddScoped(interfaceType, validatorType);
        }
    }
}

builder.Services.AddControllers();

// ══════════════════════════════════════════════════════════
// 6. CORS (para SPA React/Vue)
// ══════════════════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AlertoClients", policy =>
    {
        policy
            .WithOrigins(
                "https://tablero.alerto.gov.co",
                "https://admin.alerto.gov.co",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ══════════════════════════════════════════════════════════
// PIPELINE DE MIDDLEWARE
// ══════════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Alerto API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AlertoClients");

// Middleware personalizado de logging
// app.UseMiddleware<RequestLoggingMiddleware>();

// Middleware de manejo global de excepciones
// app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();  // ← Primero: valida JWT
app.UseAuthorization();   // ← Segundo: verifica roles/políticas

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "alerto-api",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
}));

app.Run();
