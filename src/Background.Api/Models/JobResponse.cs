using Background.Dal.Entities;

namespace Background.Api.Models;

public record JobResponse(
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
    string? SenderName,
    string? SenderAddress,
    string? Folder)
{
    public static JobResponse From(ProcessingJob m) => new(
        m.Id, m.Status.ToString(), m.RetryCount,
        m.LastStep, m.LastError, m.PipelineVersion, m.PromptId,
        m.CreatedAt, m.StartedAt, m.CompletedAt,
        m.StartedAt.HasValue && m.CompletedAt.HasValue
            ? m.CompletedAt.Value.Subtract(m.StartedAt.Value).TotalSeconds
            : null,
        m.EmailMetadata?.SenderName,
        m.EmailMetadata?.SenderAddress,
        m.EmailMetadata?.Folder);
}