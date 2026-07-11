namespace Background.AI.Abstractions;

public enum LlmFinishReason
{
    Stop,
    Length,
    ContentFilter,
    Error
}
