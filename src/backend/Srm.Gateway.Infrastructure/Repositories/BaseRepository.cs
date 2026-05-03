using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Infrastructure.Data;
using System.Linq.Expressions;

namespace Srm.Gateway.Infrastructure.Repositories;

public class BaseRepository<T>(SrmDbContext context) : IBaseRepository<T> where T : class
{
    protected readonly SrmDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<IEnumerable<T>> GetAllAsync(bool trackChanges = false) =>
        trackChanges ? await _dbSet.ToListAsync() : await _dbSet.AsNoTracking().ToListAsync();

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.AnyAsync(predicate);

    // Controlled querying to prevent leaking IQueryable directly to controllers
    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> predicate, bool trackChanges = false) =>
        trackChanges ? _dbSet.Where(predicate) : _dbSet.Where(predicate).AsNoTracking();
}