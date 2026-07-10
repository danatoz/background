namespace Background.Infrastructure.Pipeline;

public class PipelineContext
{
    public string ArtifactPrefix { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public string? PreprocessedContent { get; set; }
    public string? Prompt { get; set; }
    public string? LlmResponse { get; set; }
    public string? ProcessedJson { get; set; }
}
