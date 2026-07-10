namespace Background.Infrastructure.Storage;

public static class ArtifactPathBuilder
{
    public static string BuildPrefix(string basePrefix, Guid messageId)
    {
        var now = DateTime.UtcNow;
        return $"{basePrefix.TrimEnd('/')}/{now:yyyy'/'MM'/'dd}/{messageId:N}";
    }

    public static string Raw(string prefix) => $"{prefix}/raw.json";
    public static string Preprocessed(string prefix) => $"{prefix}/preprocessed.md";
    public static string Prompt(string prefix) => $"{prefix}/prompt.md";
    public static string LlmResponse(string prefix) => $"{prefix}/response.json";
    public static string Processed(string prefix) => $"{prefix}/processed.json";
}
