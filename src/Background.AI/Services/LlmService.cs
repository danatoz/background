using System.Diagnostics;
using Background.AI.Abstractions;
using Background.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

namespace Background.AI.Services;

internal sealed class LlmService : ILlmService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmService> _logger;

    public LlmService(Kernel kernel, IOptions<LlmOptions> options, ILogger<LlmService> logger)
    {
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmResult> ExecuteAsync(LlmRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var chatHistory = new ChatHistory();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            chatHistory.AddSystemMessage(request.SystemPrompt);

        chatHistory.AddUserMessage(request.UserPrompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = (float)(request.Temperature ?? _options.Temperature),
            MaxTokens = request.MaxTokens ?? _options.MaxTokens,
            ResponseFormat = request.ResponseFormat == LlmResponseFormat.JsonObject
                ? ChatResponseFormat.CreateJsonObjectFormat()
                : null
        };

        var result = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory, settings, null, ct);

        sw.Stop();

        var modelUsed = result.ModelId ?? request.ModelName ?? _options.ModelId;

        var promptTokens = 0;
        var completionTokens = 0;
        var totalTokens = 0;
        var finishReason = LlmFinishReason.Error;

        if (result.InnerContent is ChatCompletion completion)
        {
            if (completion.Usage is { } usage)
            {
                promptTokens = usage.InputTokenCount;
                completionTokens = usage.OutputTokenCount;
                totalTokens = usage.TotalTokenCount;
            }

            finishReason = completion.FinishReason switch
            {
                ChatFinishReason.Stop => LlmFinishReason.Stop,
                ChatFinishReason.Length => LlmFinishReason.Length,
                ChatFinishReason.ContentFilter => LlmFinishReason.ContentFilter,
                _ => LlmFinishReason.Error
            };
        }

        var content = result.Content ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            var metadata = result.Metadata is { Count: > 0 }
                ? string.Join(", ", result.Metadata.Keys)
                : "none";
            _logger.LogWarning(
                "LLM returned empty content. Model={Model}, FinishReason={FinishReason}, Duration={Duration}ms, MetadataKeys={Metadata}",
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
