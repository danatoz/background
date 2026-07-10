using Background.Dal.Entities;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Background.Infrastructure.Pipeline.Steps;

public sealed class LlmStep : IProcessingStep
{
    private readonly IStorageService _storage;
    private readonly PromptService _promptService;
    private readonly ILogger<LlmStep> _logger;

    public LlmStep(IStorageService storage, PromptService promptService, ILogger<LlmStep> logger)
    {
        _storage = storage;
        _promptService = promptService;
        _logger = logger;
    }

    public string StepName => "Llm";

    public async Task<IProcessingStepResult> ExecuteAsync(
        InboxMessage message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            var prompt = await _promptService.GetActiveAsync("inbox-classification", ct);
            if (prompt is null)
            {
                _logger.LogError("Active prompt 'inbox-classification' not found");
                return ProcessingStepResult.Fail("Active prompt 'inbox-classification' not found");
            }

            message.PromptId = prompt.Id;
            var rendered = prompt.Content.Replace("{{content}}", context.PreprocessedContent ?? context.RawContent);
            context.Prompt = rendered;

            var promptKey = ArtifactPathBuilder.Prompt(context.ArtifactPrefix);
            await _storage.SaveAsync(promptKey, rendered, "text/markdown", ct);

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
            await Task.Delay(30000, ct);
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
}
