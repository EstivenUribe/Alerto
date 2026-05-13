using Alerto.Application.Common.Interfaces;
using Alerto.Infrastructure.Authentication;
using Alerto.Infrastructure.Caching;
using Alerto.Infrastructure.HealthChecks;
using Alerto.Infrastructure.Integrations.Cap;
using Alerto.Infrastructure.Integrations.Dispatch;
using Alerto.Infrastructure.Integrations.OpenMeteo;
using Alerto.Infrastructure.Integrations.Options;
using Alerto.Infrastructure.Integrations.Publishing;
using Alerto.Infrastructure.Integrations.Siata;
using Alerto.Infrastructure.Persistence;
using Alerto.Infrastructure.Persistence.Repositories;
using Alerto.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Alerto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.Configure<MachineClientOptions>(configuration.GetSection(MachineClientOptions.SectionName));
        services.Configure<SiataOptions>(configuration.GetSection(SiataOptions.SectionName));
        services.Configure<CapGeneratorOptions>(configuration.GetSection(CapGeneratorOptions.SectionName));
        services.Configure<CellBroadcastOptions>(configuration.GetSection(CellBroadcastOptions.SectionName));
        services.Configure<NotificationPublisherOptions>(configuration.GetSection(NotificationPublisherOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<OpenMeteoOptions>(configuration.GetSection(OpenMeteoOptions.SectionName));

        services.AddDbContext<AlertoDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AlertoDb"));
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGeofenceRepository, GeofenceRepository>();
        services.AddScoped<IAuditTrailRepository, AuditTrailRepository>();
        services.AddScoped<IWeatherRepository, WeatherRepository>();
        services.AddScoped<IAlertCitizenConfirmationRepository, AlertCitizenConfirmationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IWeatherThresholdStore, WeatherThresholdStore>();

        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMachineClientValidator, MachineClientValidator>();
        services.AddScoped<IAppCache, RedisCacheService>();
        services.AddScoped<IDispatchIdempotencyStore, RedisDispatchIdempotencyStore>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddScoped<IOutboxMessageDispatcher, LoggingOutboxMessageDispatcher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenMeteoOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient<ISiataIntegrationClient, SiataIntegrationClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SiataOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient<ICapGeneratorClient, CapGeneratorClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CapGeneratorOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient<ICellBroadcastDispatcher, CellBroadcastDispatcher>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CellBroadcastOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient(nameof(SiataHealthCheck), (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SiataOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient(nameof(CapGeneratorHealthCheck), (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CapGeneratorOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient(nameof(CellBroadcastHealthCheck), (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CellBroadcastOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpContextAccessor();
        services.AddHostedService<OutboxProcessorHostedService>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready", "db"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready", "cache"])
            .AddCheck<SiataHealthCheck>("siata", tags: ["ready", "external"])
            .AddCheck<CapGeneratorHealthCheck>("cap-generator", tags: ["ready", "external"])
            .AddCheck<CellBroadcastHealthCheck>("cell-broadcast", tags: ["ready", "external"])
            .AddCheck<NotificationPublisherHealthCheck>("notification-publisher", tags: ["ready", "external", "messaging"]);

        return services;
    }
}
