using NpgsqlTypes;

namespace Background.Dal.Entities;

public enum JobStatus
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

public class ProcessingJob
{
    public Guid Id { get; set; }
    public JobStatus Status { get; set; }
    public string? LastStep { get; set; }
    public string? ArtifactPrefix { get; set; }
    public string? PipelineVersion { get; set; }
    public Guid? PromptId { get; set; }
    public Prompt? Prompt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? WorkerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }

    public EmailMetadata? EmailMetadata { get; set; }
}
