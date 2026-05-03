using System.Linq.Expressions;

namespace Srm.Gateway.Application.Interfaces;

public interface IBaseRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(bool trackChanges = false);
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    // The "Better" replacement for GetQueryable
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> predicate, bool trackChanges = false);
}