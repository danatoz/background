using Background.Dal.Entities;

namespace Background.Dal.Repositories;

public record JobListResult(List<ProcessingJob> Items, int Total);

public interface IProcessingJobRepository
{
    Task AddAsync(ProcessingJob message, CancellationToken ct = default);

    Task<ProcessingJob?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<JobListResult> GetListAsync(
        JobStatus? status = null,
        string? senderName = null,
        string? senderAddress = null,
        string? folder = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default);

    Task<List<ProcessingJob>> ClaimMessagesAsync(
        int batchSize, string workerId, TimeSpan lockDuration, CancellationToken ct = default);

    Task MarkCompletedAsync(Guid id, CancellationToken ct = default);

    Task MarkFailedAsync(
        Guid id, string error, TimeSpan? retryDelay = null, CancellationToken ct = default);

    Task ResetToPendingAsync(Guid id, CancellationToken ct = default);

    void Attach(ProcessingJob message);

    Task SaveChangesAsync(CancellationToken ct = default);
}
