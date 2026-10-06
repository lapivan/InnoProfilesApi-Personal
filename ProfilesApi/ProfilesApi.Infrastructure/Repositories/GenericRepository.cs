using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProfilesApi.Domain.Common;
using ProfilesApi.Domain.Interfaces;
using ProfilesApi.Infrastructure.Data;

namespace ProfilesApi.Infrastructure.Repositories;

public abstract class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> Entities;

    public GenericRepository(AppDbContext context)
    {
        Context = context;
        Entities = Context.Set<T>();
    }
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = Entities.AsQueryable();
        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[]? includesProperties)
    {
        IQueryable<T> query = Entities.AsQueryable();
        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (includesProperties != null)
        {
            foreach (var includeProperty in includesProperties)
            {
                query = query.Include(includeProperty);
            }
        }
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[]? includesProperties)
    {
        IQueryable<T> query = Entities.AsQueryable();
        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (includesProperties != null)
        {
            foreach (var includeProperty in includesProperties)
            {
                query = query.Include(includeProperty);
            }
        }

        var items = await query.
            OrderBy(o => o.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Delete(T entity)
    {
        Entities.Remove(entity);
    }

    public void Add(T entity)
    {
        Entities.Add(entity);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = Entities.AsQueryable();
        if (filter != null)
        {
            query = query.Where(filter);
        }
        return await query.AnyAsync(cancellationToken);
    }
}