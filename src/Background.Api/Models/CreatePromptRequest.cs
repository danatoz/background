using Background.Dal.Entities;

namespace Background.Api.Models;

public record CreatePromptRequest(
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
    JsonSchema? ResponseSchema,
    string? Provider,
    bool IsActive,
    string? FolderFilter);