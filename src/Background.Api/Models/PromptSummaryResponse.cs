using Background.Dal.Entities;

namespace Background.Api.Models;

public record PromptSummaryResponse(
    Guid Id,
    string Name,
    string Version,
    string? ModelName,
    double? Temperature,
    int? MaxTokens,
    string? ResponseFormat,
    double? TopP,
    int? Seed,
    string? Description,
    string? Tags,
    string Provider,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PromptSummaryResponse From(Prompt p) => new(
        p.Id, p.Name, p.Version, p.ModelName, p.Temperature,
        p.MaxTokens, p.ResponseFormat, p.TopP, p.Seed,
        p.Description, p.Tags,
        p.Provider, p.IsActive, p.CreatedAt, p.UpdatedAt);
}