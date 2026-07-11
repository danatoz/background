using Background.Dal.Entities;

namespace Background.Dal.Repositories;

public record MessageListResult(List<InboxMessage> Items, int Total);

public interface IInboxMessageRepository
{
    Task AddAsync(InboxMessage message, CancellationToken ct = default);

    Task<InboxMessage?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<MessageListResult> GetListAsync(
        MessageStatus? status = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default);

    Task<List<InboxMessage>> ClaimMessagesAsync(
        int batchSize, string workerId, TimeSpan lockDuration, CancellationToken ct = default);

    Task MarkCompletedAsync(Guid id, CancellationToken ct = default);

    Task MarkFailedAsync(
        Guid id, string error, TimeSpan? retryDelay = null, CancellationToken ct = default);

    Task ResetToPendingAsync(Guid id, CancellationToken ct = default);

    void Attach(InboxMessage message);

    Task SaveChangesAsync(CancellationToken ct = default);
}
