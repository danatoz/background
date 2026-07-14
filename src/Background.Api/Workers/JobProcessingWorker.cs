using Background.Dal.Entities;
using Background.Dal.Repositories;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Storage;

namespace Background.Api.Workers;

public class JobProcessingWorker : BackgroundService
{
    private static readonly SemaphoreSlim _semaphore = new(5, 5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobProcessingWorker> _logger;
    private readonly string _workerId;

    public JobProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<JobProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobProcessingWorker {WorkerId} started", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IProcessingJobRepository>();

                var messages = await repo.ClaimMessagesAsync(
                    batchSize: 5,
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

                foreach (var msg in messages)
                {
                    _ = ProcessMessageAsync(msg, stoppingToken);
                }
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

        _logger.LogInformation("JobProcessingWorker {WorkerId} stopped", _workerId);
    }

    private async Task ProcessMessageAsync(
        ProcessingJob message,
        CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IProcessingJobRepository>();
            repo.Attach(message);

            try
            {
                message.ArtifactPrefix ??= ArtifactPathBuilder.BuildPrefix("emails", message.Id);
                message.PipelineVersion ??= "1.0";

                var orchestrator = scope.ServiceProvider.GetRequiredService<PipelineOrchestrator>();
                await orchestrator.RunAsync(message, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Message {MessageId} processing was cancelled", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message {MessageId} processing failed", message.Id);

                var retryDelay = TimeSpan.FromSeconds(
                    Math.Pow(2, Math.Min(message.RetryCount, 5)) * 10);

                await repo.MarkFailedAsync(message.Id, ex.Message, retryDelay, ct);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
