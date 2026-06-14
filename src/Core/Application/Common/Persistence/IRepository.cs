namespace Retailer.Application.Common.Persistence;

/// <summary>
/// The regular read/write repository for an aggregate root.
/// </summary>
public interface IRepository<T> : IReadRepository<T>
    where T : class, IAggregateRoot
{
    Task UpdateRangeAsync(IEnumerable<T> entities, bool doSaveChanges = true);
    Task AddRangeAsync(IEnumerable<T> entities, bool doSaveChanges = true);
    Task AddAsync(T entity, bool doSaveChanges = true);
    Task DeleteAsync(T entity, bool doSaveChanges = true);
    Task DeleteRangeAsync(IEnumerable<T> enitities, bool doSaveChanges = true);
    Task UpdateAsync(T entity, bool doSaveChanges = true);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The read-only repository for an aggregate root.
/// </summary>
public interface IReadRepository<T>
    where T : class, IAggregateRoot
{
    IQueryable<T> GetAll();
    IQueryable<TResult> GetAll<TResult>();
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;
}