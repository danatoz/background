using Background.Dal.Entities;

namespace Background.Api.Models;

public record PromptSummaryResponse(
    Guid Id,
    string Name,
    string Version,
    string? ModelName,
    double? Temperature,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PromptSummaryResponse From(Prompt p) => new(
        p.Id, p.Name, p.Version, p.ModelName, p.Temperature,
        p.IsActive, p.CreatedAt, p.UpdatedAt);
}