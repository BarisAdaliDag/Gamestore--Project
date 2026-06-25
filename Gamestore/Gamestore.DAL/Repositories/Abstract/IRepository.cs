using System.Linq.Expressions;

namespace Gamestore.DAL.Repositories.Abstract;

public interface IRepository<T>
    where T : class
{
    Task<IEnumerable<T>> GetAllAsync();

    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int size,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null);

    Task<T?> GetByIdAsync(Guid id);

    Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    Task AddAsync(T entity);

    void Update(T entity);

    void Delete(T entity);
}
