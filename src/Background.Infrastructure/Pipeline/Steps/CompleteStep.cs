using Background.Dal.Entities;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Background.Infrastructure.Pipeline.Steps;

public sealed class CompleteStep : IProcessingStep
{
    private readonly IStorageService _storage;
    private readonly ILogger<CompleteStep> _logger;

    public CompleteStep(IStorageService storage, ILogger<CompleteStep> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public string StepName => "Complete";

    public async Task<IProcessingStepResult> ExecuteAsync(
        InboxMessage message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            if (context.ProcessedJson is not null)
            {
                var key = ArtifactPathBuilder.Processed(context.ArtifactPrefix);
                await _storage.SaveAsync(key, context.ProcessedJson, "application/json", ct);
                _logger.LogDebug("Saved processed artifact for {MessageId}: {Key}", message.Id, key);
            }

            return ProcessingStepResult.Done;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete step failed for {MessageId}", message.Id);
            return ProcessingStepResult.Fail(ex.Message);
        }
    }
}
