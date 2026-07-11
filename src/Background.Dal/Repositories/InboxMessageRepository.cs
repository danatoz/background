using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Background.Dal.Repositories;

internal sealed class InboxMessageRepository : IInboxMessageRepository
{
    private readonly AppDbContext _context;

    public InboxMessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InboxMessage message, CancellationToken ct = default)
    {
        await _context.Messages.AddAsync(message, ct);
    }

    public async Task<InboxMessage?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Messages.FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<MessageListResult> GetListAsync(
        MessageStatus? status = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default)
    {
        var query = _context.Messages.AsQueryable();

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

        return new MessageListResult(items, total);
    }

    public async Task<List<InboxMessage>> ClaimMessagesAsync(
        int batchSize, string workerId, TimeSpan lockDuration, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var lockedUntil = now.Add(lockDuration);

        var statusProcessing = MessageStatus.Processing.ToString().ToLowerInvariant();
        var statusPending = MessageStatus.Pending.ToString().ToLowerInvariant();
        var statusFailed = MessageStatus.Failed.ToString().ToLowerInvariant();

        var messages = await _context.Messages
            .FromSqlInterpolated($"""
                UPDATE "Messages"
                SET "Status" = {statusProcessing}::message_status,
                    "WorkerId" = {workerId},
                    "LockedUntil" = {lockedUntil},
                    "StartedAt" = {now},
                    "RetryCount" = "RetryCount" + 1
                WHERE "Id" IN (
                    SELECT "Id" FROM "Messages"
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
        await _context.Messages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, MessageStatus.Completed)
                    .SetProperty(m => m.CompletedAt, DateTime.UtcNow)
                    .SetProperty(m => m.LockedUntil, (DateTime?)null)
                    .SetProperty(m => m.WorkerId, (string?)null),
                ct);
    }

    public async Task MarkFailedAsync(
        Guid id, string error, TimeSpan? retryDelay = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _context.Messages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, MessageStatus.Failed)
                    .SetProperty(m => m.LastError, error)
                    .SetProperty(m => m.CompletedAt, now)
                    .SetProperty(m => m.LockedUntil, (DateTime?)null)
                    .SetProperty(m => m.WorkerId, (string?)null)
                    .SetProperty(m => m.NextRetryAt, retryDelay.HasValue ? now.Add(retryDelay.Value) : (DateTime?)null),
                ct);
    }

    public async Task ResetToPendingAsync(Guid id, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _context.Messages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, MessageStatus.Pending)
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

    public void Attach(InboxMessage message)
    {
        _context.Messages.Attach(message);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
