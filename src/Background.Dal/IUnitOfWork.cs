namespace Background.Dal;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<bool> CanConnectAsync(CancellationToken ct = default);
}
