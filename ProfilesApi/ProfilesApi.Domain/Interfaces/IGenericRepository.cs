using System.Linq.Expressions;
using ProfilesApi.Domain.Common;

namespace ProfilesApi.Domain.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[]? includesProperties);

    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[]? includesProperties);

    void Delete(T entity);
    void Add(T entity);
    Task<bool> ExistsAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken = default);
}