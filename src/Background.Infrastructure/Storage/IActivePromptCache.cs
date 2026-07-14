using Background.Dal.Entities;

namespace Background.Infrastructure.Storage;

public interface IActivePromptCache
{
    Task<Prompt?> GetActiveAsync(string name, CancellationToken ct = default);
    Task<Prompt?> GetActiveByFolderAsync(string folder, CancellationToken ct = default);
    void InvalidateAll();
}
