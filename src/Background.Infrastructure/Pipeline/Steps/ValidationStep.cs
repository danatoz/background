using System.Text.Json;
using Background.Dal.Entities;
using Microsoft.Extensions.Logging;

namespace Background.Infrastructure.Pipeline.Steps;

public sealed class ValidationStep : IProcessingStep
{
    private readonly ILogger<ValidationStep> _logger;

    public ValidationStep(ILogger<ValidationStep> logger)
    {
        _logger = logger;
    }

    public string StepName => "Validation";

    public Task<IProcessingStepResult> ExecuteAsync(
        InboxMessage message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(context.LlmResponse))
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("LLM response is empty"));

            using var doc = JsonDocument.Parse(context.LlmResponse);
            var root = doc.RootElement;

            if (!root.TryGetProperty("summary", out _))
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing 'summary' field in LLM response"));

            if (!root.TryGetProperty("category", out var category) || category.GetString() is null)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'category' field"));

            var validCategories = new[] { "invoice", "support", "newsletter", "meeting", "other" };
            if (!validCategories.Contains(category.GetString()))
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail($"Invalid category: {category.GetString()}"));

            context.ProcessedJson = context.LlmResponse;

            _logger.LogDebug("Validation passed for {MessageId}", message.Id);
            return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Done);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Validation failed for {MessageId}: invalid JSON", message.Id);
            return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail($"Invalid JSON: {ex.Message}"));
        }
    }
}
