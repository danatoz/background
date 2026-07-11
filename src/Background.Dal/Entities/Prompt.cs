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
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
