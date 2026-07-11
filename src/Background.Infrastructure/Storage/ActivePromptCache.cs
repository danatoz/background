using System.Collections.Concurrent;
using Background.Dal;
using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Background.Infrastructure.Storage;

public sealed class ActivePromptCache
{
    private readonly ConcurrentDictionary<string, Prompt?> _cache = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public ActivePromptCache(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<Prompt?> GetActiveAsync(string name, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(name, out var prompt))
            return prompt;

        return await ReloadAsync(name, ct);
    }

    public async Task<Prompt?> ReloadAsync(string name, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var prompt = await db.Prompts
            .Where(p => p.Name == name && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
        _cache[name] = prompt;
        return prompt;
    }

    public void Invalidate(string name)
    {
        _cache.TryRemove(name, out _);
    }
}
