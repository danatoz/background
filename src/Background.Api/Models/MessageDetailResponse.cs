using Background.Dal.Entities;

namespace Background.Api.Models;

public record MessageDetailResponse(
    Guid Id,
    string Status,
    int RetryCount,
    string? LastStep,
    string? LastError,
    string? PipelineVersion,
    Guid? PromptId,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? ProcessingDurationSeconds,
    List<ArtifactInfo> Artifacts)
{
    private static readonly Dictionary<string, string[]> ArtifactsByStep = new()
    {
        ["Preprocessing"] = new[] { "raw.json", "preprocessed.md" },
        ["Llm"] = new[] { "raw.json", "preprocessed.md", "prompt.md", "response.json" },
        ["Validation"] = new[] { "raw.json", "preprocessed.md", "prompt.md", "response.json" },
        ["Complete"] = new[] { "raw.json", "preprocessed.md", "prompt.md", "response.json", "processed.json" },
    };

    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        ["raw.json"] = "application/json",
        ["preprocessed.md"] = "text/markdown",
        ["prompt.md"] = "text/markdown",
        ["response.json"] = "application/json",
        ["processed.json"] = "application/json",
    };

    public static MessageDetailResponse From(InboxMessage m)
    {
        var artifacts = new List<ArtifactInfo>();

        if (m.ArtifactPrefix is not null)
        {
            var fileNames = m.Status == MessageStatus.Completed
                ? ArtifactsByStep["Complete"]
                : m.LastStep is not null && ArtifactsByStep.TryGetValue(m.LastStep, out var names)
                    ? names
                    : [];

            artifacts = fileNames
                .Select(f => new ArtifactInfo(f, ContentTypes.GetValueOrDefault(f, "application/octet-stream")))
                .ToList();
        }

        return new(
            m.Id, m.Status.ToString(), m.RetryCount,
            m.LastStep, m.LastError, m.PipelineVersion, m.PromptId,
            m.CreatedAt, m.StartedAt, m.CompletedAt,
            m.StartedAt.HasValue && m.CompletedAt.HasValue
                ? m.CompletedAt.Value.Subtract(m.StartedAt.Value).TotalSeconds
                : null,
            artifacts);
    }
}
