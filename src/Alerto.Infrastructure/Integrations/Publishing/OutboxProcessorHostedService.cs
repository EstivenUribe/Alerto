using Alerto.Infrastructure.Integrations.Options;
using Alerto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alerto.Infrastructure.Integrations.Publishing;

internal sealed class OutboxProcessorHostedService : BackgroundService
{
    private const string LockKey = "outbox:processor:lock";
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    public OutboxProcessorHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IConnectionMultiplexer redis,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessorHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _redis = redis;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.Value;
            if (!options.Enabled || !options.ProcessInProcess)
            {
                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
                continue;
            }

            try
            {
                await TryProcessBatchWithLockAsync(options, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox processor iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task TryProcessBatchWithLockAsync(OutboxOptions options, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var lockToken = Environment.MachineName + ":" + Environment.ProcessId;

        var acquired = await db.LockTakeAsync(LockKey, lockToken, LockExpiry);
        if (!acquired)
        {
            _logger.LogDebug("Outbox processor lock not acquired — another instance is processing.");
            return;
        }

        try
        {
            await ProcessBatchAsync(options, cancellationToken);
        }
        finally
        {
            await db.LockReleaseAsync(LockKey, lockToken);
        }
    }

    private async Task ProcessBatchAsync(OutboxOptions options, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlertoDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await dispatcher.DispatchAsync(message, cancellationToken);
                message.MarkProcessed(DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                message.MarkFailed(exception.Message, DateTime.UtcNow);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
