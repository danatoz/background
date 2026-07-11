namespace Background.Api.Models;

public record CreatePromptRequest(
    string Name,
    string Version,
    string Content,
    string? SystemPrompt,
    string? ModelName,
    double? Temperature,
    bool IsActive);