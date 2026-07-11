using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Background.Dal;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public Task<bool> CanConnectAsync(CancellationToken ct = default)
        => Database.CanConnectAsync(ct);

    public DbSet<ProcessingJob> Jobs => Set<ProcessingJob>();
    public DbSet<Prompt> Prompts => Set<Prompt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
