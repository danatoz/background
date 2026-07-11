namespace Background.Infrastructure.Pipeline;

public sealed class PipelineOptions
{
    public const string SectionName = "Pipeline";

    public int MaxRetries { get; set; } = 5;
}
