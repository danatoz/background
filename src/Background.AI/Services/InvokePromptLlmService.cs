using System.Diagnostics;
using Background.AI.Abstractions;
using Background.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

namespace Background.AI.Services;

internal sealed class InvokePromptLlmService : ILlmService
{
    private readonly Kernel _kernel;
    private readonly LlmOptions _options;
    private readonly ILogger<InvokePromptLlmService> _logger;

    public InvokePromptLlmService(Kernel kernel, IOptions<LlmOptions> options, ILogger<InvokePromptLlmService> logger)
    {
        _kernel = kernel;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmResult> ExecuteAsync(LlmRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var prompt = !string.IsNullOrWhiteSpace(request.SystemPrompt)
            ? $"{request.SystemPrompt}\n\n{request.UserPrompt}"
            : request.UserPrompt;

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = (float)(request.Temperature ?? _options.Temperature),
            MaxTokens = request.MaxTokens ?? _options.MaxTokens,
            TopP = request.TopP is not null ? (float)request.TopP : null,
            Seed = request.Seed,
            ResponseFormat = request.ResponseFormat == LlmResponseFormat.JsonObject
                ? typeof(ClassificationResult)
                : null
        };

        var arguments = new KernelArguments(settings);
        var result = await _kernel.InvokePromptAsync(prompt, arguments, cancellationToken: ct);

        sw.Stop();

        var content = result.ToString() ?? string.Empty;
        var modelUsed = request.ModelName ?? _options.ModelId;

        var promptTokens = 0;
        var completionTokens = 0;
        var totalTokens = 0;
        var finishReason = LlmFinishReason.Stop;

        if (result.Metadata?.TryGetValue("Usage", out var usageRaw) == true && usageRaw is ChatTokenUsage usage)
        {
            promptTokens = usage.InputTokenCount;
            completionTokens = usage.OutputTokenCount;
            totalTokens = usage.TotalTokenCount;
        }

        var chatMessage = result.GetValue<Microsoft.SemanticKernel.ChatMessageContent>();
        if (chatMessage?.InnerContent is ChatCompletion completion)
        {
            modelUsed = completion.Model ?? request.ModelName ?? _options.ModelId;

            finishReason = completion.FinishReason switch
            {
                ChatFinishReason.Stop => LlmFinishReason.Stop,
                ChatFinishReason.Length => LlmFinishReason.Length,
                ChatFinishReason.ContentFilter => LlmFinishReason.ContentFilter,
                _ => LlmFinishReason.Error
            };
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            var metadata = result.Metadata is { Count: > 0 }
                ? string.Join(", ", result.Metadata.Keys)
                : "none";
            _logger.LogWarning(
                "LLM returned empty content via InvokePromptAsync. Model={Model}, FinishReason={FinishReason}, Duration={Duration}ms, MetadataKeys={Metadata}",
                modelUsed, finishReason, sw.Elapsed.TotalMilliseconds, metadata);
        }

        return new LlmResult
        {
            Content = content,
            ModelUsed = modelUsed,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens,
            FinishReason = finishReason,
            Duration = sw.Elapsed
        };
    }
}
