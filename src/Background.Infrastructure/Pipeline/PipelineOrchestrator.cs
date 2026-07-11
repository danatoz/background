using Background.Dal;
using Background.Dal.Entities;
using Background.Dal.Repositories;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Background.Infrastructure.Pipeline;

public sealed class PipelineOrchestrator
{
    private readonly IReadOnlyList<IProcessingStep> _steps;
    private readonly IProcessingJobRepository _repository;
    private readonly IStorageService _storage;
    private readonly ILogger<PipelineOrchestrator> _logger;
    private readonly PipelineOptions _options;

    private static readonly int LlmStepIndex = Array.IndexOf(KnownSteps.All, "Llm");

    public PipelineOrchestrator(
        IEnumerable<IProcessingStep> steps,
        IProcessingJobRepository repository,
        IStorageService storage,
        ILogger<PipelineOrchestrator> logger,
        IOptions<PipelineOptions> options)
    {
        _steps = steps.OrderBy(s => Array.IndexOf(
            KnownSteps.All, s.StepName)).ToList();
        _repository = repository;
        _storage = storage;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RunAsync(ProcessingJob message, CancellationToken ct)
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = message.ArtifactPrefix ?? string.Empty
        };

        var startIndex = FindStartIndex(message.LastStep);

        if (message.RetryCount >= _options.MaxRetries)
        {
            _logger.LogError(
                "Message {MessageId} exceeded max retries ({MaxRetries}): {Error}",
                message.Id, _options.MaxRetries, message.LastError);

            await _repository.MarkFailedAsync(
                message.Id,
                $"Terminal failure: max retries ({_options.MaxRetries}) exceeded: {message.LastError}",
                ct: ct);
            return;
        }

        if (!string.IsNullOrEmpty(context.ArtifactPrefix))
        {
            var rawKey = ArtifactPathBuilder.Raw(context.ArtifactPrefix);
            context.RawContent = await _storage.GetAsync(rawKey, ct) ?? string.Empty;

            if (startIndex > LlmStepIndex)
            {
                var responseKey = ArtifactPathBuilder.LlmResponse(context.ArtifactPrefix);
                context.LlmResponse = await _storage.GetAsync(responseKey, ct);
            }
        }

        for (var i = startIndex; i < _steps.Count; i++)
        {
            var step = _steps[i];

            _logger.LogInformation(
                "Running step {Step}/{Total} for message {MessageId}: {StepName}",
                i + 1, _steps.Count, message.Id, step.StepName);

            var result = await step.ExecuteAsync(message, context, ct);

            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "Step {StepName} failed for message {MessageId}: {Error}",
                    step.StepName, message.Id, result.Error);

                if (result.IsTerminal)
                {
                    await _repository.MarkFailedAsync(
                        message.Id, $"Terminal failure at {step.StepName}: {result.Error}", ct: ct);
                }
                else
                {
                    var retryDelay = TimeSpan.FromSeconds(
                        Math.Pow(2, Math.Min(message.RetryCount, 5)) * 10);
                    await _repository.MarkFailedAsync(
                        message.Id, result.Error ?? "Unknown error", retryDelay, ct);
                }

                return;
            }

            message.LastStep = step.StepName;
            await _repository.SaveChangesAsync(ct);
        }

        await _repository.MarkCompletedAsync(message.Id, ct);
        _logger.LogInformation("Message {MessageId} pipeline completed", message.Id);
    }

    private static int FindStartIndex(string? currentStep)
    {
        if (string.IsNullOrEmpty(currentStep))
            return 0;

        var index = Array.IndexOf(KnownSteps.All, currentStep);
        return index >= 0 ? index + 1 : 0;
    }
}

public static class KnownSteps
{
    public static readonly string[] All =
    [
        "Preprocessing",
        "Llm",
        "Validation",
        "Complete"
    ];
}
