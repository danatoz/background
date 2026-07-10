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
}
