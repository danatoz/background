using Background.Dal;
using Background.Dal.Entities;
using Background.Dal.Repositories;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Background.Infrastructure.Pipeline;

public sealed class PipelineOrchestrator
{
    private readonly IReadOnlyList<IProcessingStep> _steps;
    private readonly IInboxMessageRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PipelineOrchestrator> _logger;

    public PipelineOrchestrator(
        IEnumerable<IProcessingStep> steps,
        IInboxMessageRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PipelineOrchestrator> logger)
    {
        _steps = steps.OrderBy(s => Array.IndexOf(
            KnownSteps.All, s.StepName)).ToList();
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RunAsync(InboxMessage message, CancellationToken ct)
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = message.ArtifactPrefix ?? string.Empty,
            RawContent = message.Payload
        };

        var startIndex = FindStartIndex(message.LastStep);

        for (var i = startIndex; i < _steps.Count; i++)
        {
            var step = _steps[i];

            _logger.LogInformation(
                "Running step {Step}/{Total} for message {MessageId}: {StepName}",
                i + 1, _steps.Count, message.Id, step.StepName);

            message.LastStep = step.StepName;
            await _repository.SaveChangesAsync(ct);

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

                return;
            }
        }

        await _repository.MarkCompletedAsync(message.Id, ct);
        _logger.LogInformation("Message {MessageId} pipeline completed", message.Id);
    }

    private int FindStartIndex(string? currentStep)
    {
        if (string.IsNullOrEmpty(currentStep))
            return 0;

        var index = Array.IndexOf(KnownSteps.All, currentStep);
        return index >= 0 ? index : 0;
    }
}

public static class KnownSteps
{
    public static readonly string[] All =
    [
        "RawStorage",
        "Preprocessing",
        "Llm",
        "Validation",
        "Complete"
    ];
}
