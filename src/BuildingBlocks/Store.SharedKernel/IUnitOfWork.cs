namespace Store.SharedKernel;

/// <summary>Commits all changes of a business transaction atomically.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
