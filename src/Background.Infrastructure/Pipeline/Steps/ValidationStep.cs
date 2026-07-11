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

            if (!root.TryGetProperty("client_name", out var clientName) || clientName.GetString() is null)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'client_name' field"));

            if (!root.TryGetProperty("client_inn", out var clientInn) || clientInn.GetString() is null)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'client_inn' field"));

            if (!root.TryGetProperty("document_type", out var docType) || docType.GetString() is null)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'document_type' field"));

            if (!root.TryGetProperty("delivery_amount", out var amount) || amount.ValueKind != JsonValueKind.Object)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'delivery_amount' field"));

            if (!amount.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Number)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'delivery_amount.value' field"));

            if (!amount.TryGetProperty("currency", out var currency) || currency.GetString() is null)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'delivery_amount.currency' field"));

            if (!root.TryGetProperty("confidence", out var confidence) || confidence.ValueKind != JsonValueKind.Number)
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Fail("Missing or invalid 'confidence' field"));

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
