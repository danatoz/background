using Background.AI.Abstractions;
using Background.AI.Configuration;
using Background.Dal.Entities;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Background.Infrastructure.Pipeline.Steps;

public sealed class LlmStep : IProcessingStep
{
    private readonly IStorageService _storage;
    private readonly IActivePromptCache _promptCache;
    private readonly ILlmService _llmService;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmStep> _logger;

    public LlmStep(
        IStorageService storage,
        IActivePromptCache promptCache,
        ILlmService llmService,
        IOptions<LlmOptions> options,
        ILogger<LlmStep> logger)
    {
        _storage = storage;
        _promptCache = promptCache;
        _llmService = llmService;
        _options = options.Value;
        _logger = logger;
    }

    public string StepName => "Llm";

    public async Task<IProcessingStepResult> ExecuteAsync(
        ProcessingJob message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            Prompt? prompt = null;

            if (!string.IsNullOrEmpty(message.EmailMetadata?.Folder))
                prompt = await _promptCache.GetActiveByFolderAsync(message.EmailMetadata.Folder, ct);

            prompt ??= await _promptCache.GetActiveAsync(_options.PromptName, ct);

            if (prompt is null)
            {
                _logger.LogError("Active prompt '{PromptName}' not found", _options.PromptName);
                return ProcessingStepResult.Fail($"Active prompt '{_options.PromptName}' not found");
            }

            message.PromptId = prompt.Id;
            context.ResponseSchema = prompt.ResponseSchema?.Raw;
            var rendered = prompt.Content.Replace("{{content}}", context.PreprocessedContent ?? context.RawContent);
            context.Prompt = rendered;

            var promptKey = ArtifactPathBuilder.Prompt(context.ArtifactPrefix);
            await _storage.SaveAsync(promptKey, rendered, "text/markdown", ct);

            var responseFormat = prompt.ResponseFormat switch
            {
                "JsonObject" => LlmResponseFormat.JsonObject,
                "Text" => LlmResponseFormat.Text,
                _ => _options.UseStructuredOutput
                    ? LlmResponseFormat.JsonObject
                    : LlmResponseFormat.Text
            };

            var llmResult = await _llmService.ExecuteAsync(new LlmRequest
            {
                SystemPrompt = prompt.SystemPrompt ?? string.Empty,
                UserPrompt = rendered,
                ModelName = prompt.ModelName,
                Temperature = prompt.Temperature,
                MaxTokens = prompt.MaxTokens,
                TopP = prompt.TopP,
                Seed = prompt.Seed,
                ResponseSchema = prompt.ResponseSchema?.Raw,
                ResponseFormat = responseFormat,
                Provider = prompt.Provider,
            }, ct);

            context.LlmResponse = llmResult.Content;

            var responseKey = ArtifactPathBuilder.LlmResponse(context.ArtifactPrefix);
            await _storage.SaveAsync(responseKey, llmResult.Content, "application/json", ct);

            _logger.LogInformation(
                "LLM call: model={Model}, {PromptTokens} in + {CompletionTokens} out, {FinishReason}, {Duration}ms",
                llmResult.ModelUsed, llmResult.PromptTokens, llmResult.CompletionTokens,
                llmResult.FinishReason, llmResult.Duration.TotalMilliseconds);

            if (string.IsNullOrWhiteSpace(llmResult.Content))
            {
                _logger.LogError(
                    "LLM returned empty response for {MessageId} (finish reason: {FinishReason})",
                    message.Id, llmResult.FinishReason);
                return ProcessingStepResult.Fail("LLM returned empty response");
            }

            return ProcessingStepResult.Done;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM step failed for {MessageId}", message.Id);
            return ProcessingStepResult.Fail(ex.Message);
        }
    }
}
