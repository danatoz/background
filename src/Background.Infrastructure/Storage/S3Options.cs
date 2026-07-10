namespace Background.Infrastructure.Storage;

public sealed class S3Options
{
    public const string Section = "S3";

    public string ServiceUrl { get; init; } = "http://127.0.0.1:9000";
    public string BucketName { get; init; } = "inbox";
    public string AccessKey { get; init; } = "minioadmin";
    public string SecretKey { get; init; } = "minioadmin";
    public bool ForcePathStyle { get; init; } = true;
}
