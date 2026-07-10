using NpgsqlTypes;

namespace Background.Dal.Entities;

public enum MessageStatus
{
    [PgName("pending")]
    Pending,
    [PgName("processing")]
    Processing,
    [PgName("completed")]
    Completed,
    [PgName("failed")]
    Failed
}

public class InboxMessage
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public string? LastStep { get; set; }
    public string? ArtifactPrefix { get; set; }
    public string? PipelineVersion { get; set; }
    public string? PromptVersion { get; set; }
    public string? ModelName { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? WorkerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
