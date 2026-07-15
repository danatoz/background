using System.Text.Json.Nodes;
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
    int? MaxTokens,
    string? ResponseFormat,
    double? TopP,
    int? Seed,
    string? Description,
    string? Tags,
    JsonNode? ResponseSchema,
    string? FolderFilter,
    string Provider,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PromptDetailResponse From(Prompt p) => new(
        p.Id, p.Name, p.Version, p.Content, p.SystemPrompt,
        p.ModelName, p.Temperature, p.MaxTokens, p.ResponseFormat, p.TopP, p.Seed,
        p.Description, p.Tags, p.ResponseSchema, p.FolderFilter,
        p.Provider, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
