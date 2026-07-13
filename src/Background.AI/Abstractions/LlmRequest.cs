namespace Background.AI.Abstractions;

public record LlmRequest
{
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public string? ModelName { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public double? TopP { get; init; }
    public int? Seed { get; init; }
    public LlmResponseFormat ResponseFormat { get; init; } = LlmResponseFormat.Text;
    public string Provider { get; init; } = "ChatCompletion";
}
