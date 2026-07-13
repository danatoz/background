namespace Background.AI.Configuration;

public class LlmOptions
{
    public const string Section = "Llm";

    public string ModelId { get; init; } = "gpt-4o-mini";
    public string ApiKey { get; init; } = string.Empty;
    public string? Endpoint { get; init; }
    public double Temperature { get; init; } = 0.0;
    public int? MaxTokens { get; init; } = 1024;
    public bool UseStructuredOutput { get; init; } = true;
    public string PromptName { get; init; } = "inbox-classification";
    public int TimeoutSeconds { get; init; } = 300;
}
