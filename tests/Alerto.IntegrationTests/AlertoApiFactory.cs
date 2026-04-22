using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Alerto.IntegrationTests;

public sealed class AlertoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private RedisContainer? _redisContainer;

    public string? DockerUnavailableReason { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:AlertoDb"] = _postgresContainer?.GetConnectionString() ?? "Host=localhost;Port=5432;Database=alerto_integration;Username=postgres;Password=postgres",
                ["ConnectionStrings:Redis"] = _redisContainer?.GetConnectionString() ?? "localhost:6379,abortConnect=false",
                ["BootstrapAdmin:Username"] = "admin",
                ["BootstrapAdmin:DisplayName"] = "Administrador Test",
                ["BootstrapAdmin:Email"] = "admin@test.local",
                ["BootstrapAdmin:Password"] = "AlertoAdmin123!",
                ["Jwt:Issuer"] = "alerto-test",
                ["Jwt:Audience"] = "alerto-test-clients",
                ["Jwt:SecretKey"] = "Alerto.Integration.Tests.Secret.Key.2026",
                ["MachineClients:Clients:0:ClientId"] = "rules-engine",
                ["MachineClients:Clients:0:ClientSecret"] = "rules-engine-secret",
                ["MachineClients:Clients:0:DisplayName"] = "Rules Engine Test",
                ["MachineClients:Clients:0:Role"] = "RulesEngine",
                ["MachineClients:Clients:0:Scope"] = "alerts:read alerts:dispatch"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });
    }

    public async Task InitializeAsync()
    {
        try
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("alerto_integration")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            _redisContainer = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();

            await _postgresContainer.StartAsync();
            await _redisContainer.StartAsync();
        }
        catch (Exception exception)
        {
            DockerUnavailableReason = exception.Message;
        }
    }

    public new async Task DisposeAsync()
    {
        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }

        if (_redisContainer is not null)
        {
            await _redisContainer.DisposeAsync();
        }
    }

    public bool IsDockerAvailable => string.IsNullOrWhiteSpace(DockerUnavailableReason);
}
