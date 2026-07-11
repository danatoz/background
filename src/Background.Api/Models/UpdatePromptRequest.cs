namespace Background.Api.Models;

public record UpdatePromptRequest(
    string Name,
    string Version,
    string Content,
    string? SystemPrompt,
    string? ModelName,
    double? Temperature,
    bool IsActive);