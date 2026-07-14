using System.Text.Json;
using System.Text.Json.Nodes;
using Background.Dal.Entities;
using Json.Schema;
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
        ProcessingJob message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            var raw = context.LlmResponse;
            if (string.IsNullOrWhiteSpace(raw))
                return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("LLM response is empty"));

            var firstBrace = raw.IndexOf('{');
            var lastBrace = raw.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                raw = raw[firstBrace..(lastBrace + 1)];
                context.LlmResponse = raw;
            }

            using var doc = JsonDocument.Parse(raw);

            if (!string.IsNullOrEmpty(context.ResponseSchema))
            {
                var result = ValidateAgainstSchema(raw, context.ResponseSchema);
                if (result is not null)
                    return Task.FromResult<IProcessingStepResult>(result);
            }
            else
            {
                var root = doc.RootElement;

                if (!root.TryGetProperty("client_name", out var clientName) || clientName.GetString() is null)
                    return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("Missing or invalid 'client_name' field"));

                if (!root.TryGetProperty("client_inn", out var clientInn) || clientInn.GetString() is null)
                    return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("Missing or invalid 'client_inn' field"));

                if (!root.TryGetProperty("document_type", out var docType) || docType.GetString() is null)
                    return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("Missing or invalid 'document_type' field"));

                if (!root.TryGetProperty("delivery_amount", out var amount) || amount.ValueKind != JsonValueKind.Object)
                    return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("Missing or invalid 'delivery_amount' field"));

                if (!amount.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Number)
                    return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("Missing or invalid 'delivery_amount.value' field"));

                if (!amount.TryGetProperty("currency", out var currency) || currency.GetString() is null)
                    return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("Missing or invalid 'delivery_amount.currency' field"));

                if (!root.TryGetProperty("confidence", out var confidence) || confidence.ValueKind != JsonValueKind.Number)
                    return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail("Missing or invalid 'confidence' field"));
            }

            context.ProcessedJson = context.LlmResponse;

            _logger.LogDebug("Validation passed for {MessageId}", message.Id);
            return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.Done);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Validation failed for {MessageId}: invalid JSON", message.Id);
            return Task.FromResult<IProcessingStepResult>(ProcessingStepResult.TerminalFail($"Invalid JSON: {ex.Message}"));
        }
    }

    private static ProcessingStepResult? ValidateAgainstSchema(string json, string schemaJson)
    {
        JsonSchema schema;
        try
        {
            var normalized = NormalizeSchema(schemaJson);
            schema = JsonSchema.FromText(normalized);
        }
        catch (Exception ex)
        {
            return ProcessingStepResult.TerminalFail($"Invalid ResponseSchema: {ex.Message}");
        }

        JsonElement instance;
        try
        {
            instance = JsonDocument.Parse(json).RootElement;
        }
        catch (Exception ex)
        {
            return ProcessingStepResult.TerminalFail($"Invalid JSON instance: {ex.Message}");
        }

        var result = schema.Evaluate(instance);
        if (result.IsValid)
            return null;

        var errors = new List<string>();
        CollectErrors(result, errors);
        var detail = errors.Count > 0
            ? string.Join("; ", errors)
            : "Unknown schema validation error";

        return ProcessingStepResult.TerminalFail($"Schema validation failed: {detail}");
    }

    private static string NormalizeSchema(string schemaJson)
    {
        var node = JsonNode.Parse(schemaJson);
        if (node is JsonObject obj && obj.ContainsKey("$schema"))
        {
            obj.Remove("$schema");
            return obj.ToJsonString();
        }
        return schemaJson;
    }

    private static void CollectErrors(EvaluationResults results, List<string> errors)
    {
        if (results.Errors is { Count: > 0 })
        {
            var location = results.InstanceLocation.ToString();
            foreach (var kvp in results.Errors)
                errors.Add($"{location}: {kvp.Value}");
        }

        if (results.Details is { Count: > 0 })
        {
            foreach (var detail in results.Details)
                CollectErrors(detail, errors);
        }
    }
}
