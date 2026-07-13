using System.Diagnostics;
using System.Text.Json;
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
            TopP = request.TopP is not null ? (float)request.TopP : null,
            Seed = request.Seed,
            ResponseFormat = request.ResponseFormat == LlmResponseFormat.JsonObject
                ? request.ResponseSchema is not null
                    ? ChatResponseFormat.CreateJsonSchemaFormat(
                        "response", BinaryData.FromString(request.ResponseSchema))
                    : typeof(ClassificationResult)
                : null
        };

        _logger.LogInformation(
            "LLM request: model={Model}, temp={Temp}, maxTokens={Max}, topP={TopP}, seed={Seed}, jsonMode={Json}",
            request.ModelName ?? _options.ModelId,
            settings.Temperature, settings.MaxTokens,
            settings.TopP, settings.Seed,
            settings.ResponseFormat is not null);

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

        var content = result.Content;
        if (string.IsNullOrWhiteSpace(content))
            content = string.Concat(result.Items.OfType<TextContent>());

        if (string.IsNullOrWhiteSpace(content))
            content = result.ToString();

        if (string.IsNullOrWhiteSpace(content) && result.InnerContent is ChatCompletion c)
        {
            try
            {
                var json = JsonSerializer.Serialize(c);
                using var rawDoc = JsonDocument.Parse(json);
                if (rawDoc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("reasoning_content", out var rc) &&
                    rc.ValueKind == JsonValueKind.String)
                {
                    content = rc.GetString() ?? string.Empty;
                    _logger.LogDebug("Extracted content from reasoning_content ({Length} chars)", content.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract reasoning_content");
            }
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            var metadata = result.Metadata is { Count: > 0 }
                ? string.Join(", ", result.Metadata.Keys)
                : "none";
            var itemTypes = result.Items.Count > 0
                ? string.Join(", ", result.Items.Select(i => i.GetType().Name))
                : "none";
            _logger.LogWarning(
                "LLM returned empty content. Model={Model}, FinishReason={FinishReason}, Duration={Duration}ms, MetadataKeys={Metadata}, ItemTypes={ItemTypes}",
                modelUsed, finishReason, sw.Elapsed.TotalMilliseconds, metadata, itemTypes);
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
