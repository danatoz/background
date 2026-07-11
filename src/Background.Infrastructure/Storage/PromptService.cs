using Background.Dal;
using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Background.Infrastructure.Storage;

public class PromptService
{
    private readonly AppDbContext _db;
    private readonly ActivePromptCache _cache;

    public PromptService(AppDbContext db, ActivePromptCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<List<Prompt>> GetAllAsync(CancellationToken ct = default)
    {
        return _db.Prompts
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<Prompt?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Prompts.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Prompt> CreateAsync(Prompt prompt, CancellationToken ct = default)
    {
        prompt.Id = Guid.NewGuid();
        prompt.CreatedAt = DateTime.UtcNow;

        if (prompt.IsActive)
            await DeactivateOthersAsync(prompt.Name, null, ct);

        _db.Prompts.Add(prompt);
        await _db.SaveChangesAsync(ct);
        _cache.Invalidate(prompt.Name);
        return prompt;
    }

    public async Task<Prompt> UpdateAsync(Prompt existing, Prompt updated, CancellationToken ct = default)
    {
        existing.Name = updated.Name;
        existing.Version = updated.Version;
        existing.Content = updated.Content;
        existing.SystemPrompt = updated.SystemPrompt;
        existing.ModelName = updated.ModelName;
        existing.Temperature = updated.Temperature;

        if (updated.IsActive && !existing.IsActive)
            await DeactivateOthersAsync(existing.Name, existing.Id, ct);

        existing.IsActive = updated.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        _cache.Invalidate(existing.Name);
        return existing;
    }

    private async Task DeactivateOthersAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var others = await _db.Prompts
            .Where(p => p.Name == name && p.IsActive)
            .Where(p => excludeId == null || p.Id != excludeId)
            .ToListAsync(ct);

        foreach (var p in others)
            p.IsActive = false;
    }
}
