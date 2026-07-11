using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Background.Dal.Repositories;

internal sealed class ProcessingJobRepository : IProcessingJobRepository
{
    private readonly AppDbContext _context;

    public ProcessingJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProcessingJob message, CancellationToken ct = default)
    {
        await _context.Jobs.AddAsync(message, ct);
    }

    public async Task<ProcessingJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Jobs.FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<JobListResult> GetListAsync(
        JobStatus? status = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default)
    {
        var query = _context.Jobs.AsQueryable();

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (createdFrom.HasValue)
            query = query.Where(m => m.CreatedAt >= createdFrom.Value);

        if (createdTo.HasValue)
            query = query.Where(m => m.CreatedAt <= createdTo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return new JobListResult(items, total);
    }

    public async Task<List<ProcessingJob>> ClaimMessagesAsync(
        int batchSize, string workerId, TimeSpan lockDuration, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var lockedUntil = now.Add(lockDuration);

        var statusProcessing = JobStatus.Processing.ToString().ToLowerInvariant();
        var statusPending = JobStatus.Pending.ToString().ToLowerInvariant();
        var statusFailed = JobStatus.Failed.ToString().ToLowerInvariant();

        var messages = await _context.Jobs
            .FromSqlInterpolated($"""
                UPDATE "ProcessingJobs"
                SET "Status" = {statusProcessing}::message_status,
                    "WorkerId" = {workerId},
                    "LockedUntil" = {lockedUntil},
                    "StartedAt" = {now}
                WHERE "Id" IN (
                    SELECT "Id" FROM "ProcessingJobs"
                    WHERE ("Status" = {statusPending}::message_status
                        OR ("Status" = {statusFailed}::message_status AND "NextRetryAt" <= {now})
                        OR ("Status" = {statusProcessing}::message_status AND "LockedUntil" <= {now}))
                      AND ("LockedUntil" IS NULL OR "LockedUntil" <= {now})
                    ORDER BY "CreatedAt" ASC
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING *
                """)
            .ToListAsync(ct);

        return messages;
    }

    public async Task MarkCompletedAsync(Guid id, CancellationToken ct = default)
    {
        await _context.Jobs
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, JobStatus.Completed)
                    .SetProperty(m => m.CompletedAt, DateTime.UtcNow)
                    .SetProperty(m => m.LockedUntil, (DateTime?)null)
                    .SetProperty(m => m.WorkerId, (string?)null),
                ct);
    }

    public async Task MarkFailedAsync(
        Guid id, string error, TimeSpan? retryDelay = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _context.Jobs
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, JobStatus.Failed)
                    .SetProperty(m => m.LastError, error)
                    .SetProperty(m => m.RetryCount, m => m.RetryCount + 1)
                    .SetProperty(m => m.CompletedAt, now)
                    .SetProperty(m => m.LockedUntil, (DateTime?)null)
                    .SetProperty(m => m.WorkerId, (string?)null)
                    .SetProperty(m => m.NextRetryAt, retryDelay.HasValue ? now.Add(retryDelay.Value) : (DateTime?)null),
                ct);
    }

    public async Task ResetToPendingAsync(Guid id, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _context.Jobs
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, JobStatus.Pending)
                    .SetProperty(m => m.RetryCount, 0)
                    .SetProperty(m => m.LastStep, (string?)null)
                    .SetProperty(m => m.LastError, (string?)null)
                    .SetProperty(m => m.NextRetryAt, (DateTime?)null)
                    .SetProperty(m => m.LockedUntil, (DateTime?)null)
                    .SetProperty(m => m.WorkerId, (string?)null)
                    .SetProperty(m => m.StartedAt, (DateTime?)null)
                    .SetProperty(m => m.CompletedAt, (DateTime?)null)
                    .SetProperty(m => m.PromptId, (Guid?)null),
                ct);
    }

    public void Attach(ProcessingJob message)
    {
        _context.Jobs.Attach(message);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
