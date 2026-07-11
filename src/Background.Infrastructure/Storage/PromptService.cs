using Background.Dal;
using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Background.Infrastructure.Storage;

public class PromptService
{
    private readonly AppDbContext _db;

    public PromptService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Prompt?> GetActiveAsync(string name, CancellationToken ct = default)
    {
        return await _db.Prompts
            .Where(p => p.Name == name && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
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
        _db.Prompts.Add(prompt);
        await _db.SaveChangesAsync(ct);
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
        existing.IsActive = updated.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return existing;
    }
}
