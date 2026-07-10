using Background.Dal.Entities;
using Background.Dal.Repositories;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Storage;

namespace Background.Api.Workers;

public class InboxProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InboxProcessingWorker> _logger;
    private readonly string _workerId;

    public InboxProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<InboxProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InboxProcessingWorker {WorkerId} started", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();

                var messages = await repo.ClaimMessagesAsync(
                    batchSize: 10,
                    workerId: _workerId,
                    lockDuration: TimeSpan.FromMinutes(5),
                    ct: stoppingToken);

                if (messages.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                _logger.LogInformation(
                    "Claimed {Count} messages for processing", messages.Count);

                var processingTasks = messages.Select(msg => ProcessMessageAsync(scope, repo, msg, stoppingToken));
                await Task.WhenAll(processingTasks);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in processing loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("InboxProcessingWorker {WorkerId} stopped", _workerId);
    }

    private async Task ProcessMessageAsync(
        IServiceScope scope,
        IInboxMessageRepository repo,
        InboxMessage message,
        CancellationToken ct)
    {
        try
        {
            message.ArtifactPrefix ??= ArtifactPathBuilder.BuildPrefix("emails", message.Id);
            message.PipelineVersion ??= "1.0";

            var orchestrator = scope.ServiceProvider.GetRequiredService<PipelineOrchestrator>();
            await orchestrator.RunAsync(message, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message {MessageId} processing failed", message.Id);

            var retryDelay = TimeSpan.FromSeconds(
                Math.Pow(2, Math.Min(message.RetryCount, 5)) * 10);

            await repo.MarkFailedAsync(message.Id, ex.Message, retryDelay, ct);
        }
    }
}
