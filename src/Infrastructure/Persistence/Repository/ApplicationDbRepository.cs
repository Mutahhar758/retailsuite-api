using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Retailer.Application.Common.Persistence;
using Retailer.Domain.Common.Contracts;
using Retailer.Infrastructure.Persistence.Context;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Persistence.Repository;

public class ApplicationDbRepository<T> : IRepository<T>
    where T : class, IAggregateRoot
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _entities;

    public ApplicationDbRepository(ApplicationDbContext dbContext)
    {
        _context = dbContext;
        _context.EnforceMultiTenantOnTracking();
        _entities = dbContext.Set<T>();
    }

    public IQueryable<T> GetAll()
    {
        return _entities;
    }

    public IQueryable<TResult> GetAll<TResult>()
    {
        return _entities.ProjectToType<TResult>();
    }

    public async Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
        where TId : notnull
    {
        return await _entities
            .Where(e => ((BaseEntity<TId>)(object)e).Id!.Equals(id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, bool doSaveChanges = true)
    {
        _entities.UpdateRange(entities);

        if (doSaveChanges)
            await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, bool doSaveChanges = true)
    {
        _entities.AddRange(entities);

        if (doSaveChanges)
            await _context.SaveChangesAsync();
    }

    public async Task AddAsync(T entity, bool doSaveChanges = true)
    {
        _entities.Add(entity);

        if (doSaveChanges)
            await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity, bool doSaveChanges = true)
    {
        _entities.Update(entity);

        if (doSaveChanges)
            await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity, bool doSaveChanges = true)
    {
        _entities.Remove(entity);

        if (doSaveChanges)
            await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<T> enitities, bool doSaveChanges = true)
    {
        _entities.RemoveRange(enitities);

        if (doSaveChanges)
            await _context.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}