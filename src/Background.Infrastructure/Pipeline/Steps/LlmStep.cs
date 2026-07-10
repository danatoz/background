using Background.Dal.Entities;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Background.Infrastructure.Pipeline.Steps;

public sealed class LlmStep : IProcessingStep
{
    private readonly IStorageService _storage;
    private readonly ILogger<LlmStep> _logger;

    public LlmStep(IStorageService storage, ILogger<LlmStep> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public string StepName => "Llm";

    public async Task<IProcessingStepResult> ExecuteAsync(
        InboxMessage message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            context.Prompt = BuildPrompt(context.PreprocessedContent ?? string.Empty);

            var promptKey = ArtifactPathBuilder.Prompt(context.ArtifactPrefix);
            await _storage.SaveAsync(promptKey, context.Prompt, "text/markdown", ct);

            // TODO: Replace with actual LLM call
            context.LlmResponse = $$"""
                {
                  "summary": "Test message with HTML content - LLM integration pending",
                  "category": "other",
                  "priority": "normal",
                  "is_action_required": false,
                  "due_date": null
                }
                """;

            var responseKey = ArtifactPathBuilder.LlmResponse(context.ArtifactPrefix);
            await _storage.SaveAsync(responseKey, context.LlmResponse, "application/json", ct);

            _logger.LogDebug("LLM artifacts saved for {MessageId}", message.Id);
            return ProcessingStepResult.Done;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM step failed for {MessageId}", message.Id);
            return ProcessingStepResult.Fail(ex.Message);
        }
    }

    private static string BuildPrompt(string content)
    {
        return $"""
            Analyze the following message and extract structured information.
            Return a JSON object with:
            - summary: brief summary of the message
            - category: one of [invoice, support, newsletter, meeting, other]
            - priority: one of [low, normal, high, urgent]
            - is_action_required: boolean
            - due_date: ISO date string or null

            Message:
            {content}
            """;
    }
}
