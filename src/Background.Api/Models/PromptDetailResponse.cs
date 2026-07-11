using Background.Dal.Entities;

namespace Background.Api.Models;

public record PromptDetailResponse(
    Guid Id,
    string Name,
    string Version,
    string Content,
    string? SystemPrompt,
    string? ModelName,
    double? Temperature,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PromptDetailResponse From(Prompt p) => new(
        p.Id, p.Name, p.Version, p.Content, p.SystemPrompt,
        p.ModelName, p.Temperature, p.IsActive, p.CreatedAt, p.UpdatedAt);
}