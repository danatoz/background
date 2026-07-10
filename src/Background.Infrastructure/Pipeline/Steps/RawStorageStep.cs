using Background.Dal.Entities;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Background.Infrastructure.Pipeline.Steps;

public sealed class RawStorageStep : IProcessingStep
{
    private readonly IStorageService _storage;
    private readonly ILogger<RawStorageStep> _logger;

    public RawStorageStep(IStorageService storage, ILogger<RawStorageStep> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public string StepName => "RawStorage";

    public async Task<IProcessingStepResult> ExecuteAsync(
        InboxMessage message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            var key = ArtifactPathBuilder.Raw(context.ArtifactPrefix);
            await _storage.SaveAsync(key, context.RawContent, "application/json", ct);

            _logger.LogDebug("Saved raw artifact for {MessageId}: {Key}", message.Id, key);
            return ProcessingStepResult.Done;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save raw artifact for {MessageId}", message.Id);
            return ProcessingStepResult.Fail(ex.Message);
        }
    }
}
