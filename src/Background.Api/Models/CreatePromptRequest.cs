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
    string? ResponseSchema,
    string? Provider,
    bool IsActive);