namespace Background.Infrastructure.Storage;

public interface IStorageService
{
    Task SaveAsync(string key, string content, string contentType, CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
