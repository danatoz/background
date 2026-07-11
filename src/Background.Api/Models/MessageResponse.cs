using Background.Dal.Entities;

namespace Background.Api.Models;

public record MessageResponse(
    Guid Id,
    string Payload,
    string Status,
    int RetryCount,
    string? LastStep,
    string? LastError,
    string? PipelineVersion,
    Guid? PromptId,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? ProcessingDurationSeconds)
{
    public static MessageResponse From(InboxMessage m) => new(
        m.Id, m.Payload, m.Status.ToString(), m.RetryCount,
        m.LastStep, m.LastError, m.PipelineVersion, m.PromptId,
        m.CreatedAt, m.StartedAt, m.CompletedAt,
        m.StartedAt.HasValue && m.CompletedAt.HasValue
            ? m.CompletedAt.Value.Subtract(m.StartedAt.Value).TotalSeconds
            : null);
}