namespace Background.Dal.Entities;

public class Prompt
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public string? ModelName { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string? ResponseFormat { get; set; }
    public double? TopP { get; set; }
    public int? Seed { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public string? ResponseSchema { get; set; }
    public string? FolderFilter { get; set; }
    public string Provider { get; set; } = "ChatCompletion";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
