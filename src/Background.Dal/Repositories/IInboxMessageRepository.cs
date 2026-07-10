using Background.Dal.Entities;

namespace Background.Dal.Repositories;

public interface IInboxMessageRepository
{
    Task AddAsync(InboxMessage message, CancellationToken ct = default);

    Task<InboxMessage?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<List<InboxMessage>> ClaimMessagesAsync(
        int batchSize, string workerId, TimeSpan lockDuration, CancellationToken ct = default);

    Task MarkCompletedAsync(Guid id, CancellationToken ct = default);

    Task MarkFailedAsync(
        Guid id, string error, TimeSpan? retryDelay = null, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
