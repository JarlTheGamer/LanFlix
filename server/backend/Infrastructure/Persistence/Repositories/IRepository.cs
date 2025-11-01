using System.Linq.Expressions;

namespace Lanflix.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository interface for common data access operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the predicate
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity
    /// </summary>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a queryable for complex queries
    /// </summary>
    IQueryable<T> Query();
}
