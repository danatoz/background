namespace Background.AI.Abstractions;

public record LlmResult
{
    public string Content { get; init; } = string.Empty;
    public string ModelUsed { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
    public LlmFinishReason FinishReason { get; init; }
    public TimeSpan Duration { get; init; }
}
