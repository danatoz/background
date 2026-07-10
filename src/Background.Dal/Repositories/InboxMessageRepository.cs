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
                        OR ("Status" = {statusFailed}::message_status AND "NextRetryAt" <= {now}))
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

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
