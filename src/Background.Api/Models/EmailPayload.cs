using System.Text.Json.Serialization;

namespace Background.Api.Models;

public class EmailPayload
{
    [JsonPropertyName("senderName")]
    public string? SenderName { get; set; }

    [JsonPropertyName("senderAddress")]
    public string? SenderAddress { get; set; }

    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    [JsonPropertyName("body")]
    public EmailBody? Body { get; set; }

    [JsonPropertyName("attachments")]
    public List<EmailAttachment>? Attachments { get; set; }
}

public class EmailBody
{
    [JsonPropertyName("isHtml")]
    public bool IsHtml { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("html")]
    public string? Html { get; set; }

    [JsonPropertyName("s3Key")]
    public string? S3Key { get; set; }
}

public class EmailAttachment
{
    [JsonPropertyName("s3Key")]
    public string? S3Key { get; set; }
}
