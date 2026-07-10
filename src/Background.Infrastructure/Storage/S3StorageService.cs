using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Background.Infrastructure.Storage;

public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly S3Options _options;

    public S3StorageService(IAmazonS3 s3, IOptions<S3Options> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task SaveAsync(string key, string content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            ContentBody = content,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request, ct);
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_options.BucketName, key, ct);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync(ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _s3.DeleteObjectAsync(_options.BucketName, key, ct);
    }
}
