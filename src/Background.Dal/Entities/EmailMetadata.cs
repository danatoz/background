namespace Background.Dal.Entities;

public class EmailMetadata
{
    public Guid Id { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAddress { get; set; }
    public string? Folder { get; set; }
    public bool? BodyIsHtml { get; set; }
    public string? BodyS3Key { get; set; }
    public string? AttachmentsJson { get; set; }

    public ProcessingJob Job { get; set; } = null!;
}
