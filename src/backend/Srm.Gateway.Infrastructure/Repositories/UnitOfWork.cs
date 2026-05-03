using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Infrastructure.Data;
using Srm.Gateway.Infrastructure.Repositories;
using System.Collections.Concurrent;

namespace Srm.Gateway.Infrastructure;

public class UnitOfWork(SrmDbContext context) : IUnitOfWork
{
    private readonly SrmDbContext _context = context;
    // Using ConcurrentDictionary for better thread-safety during instantiation
    private readonly ConcurrentDictionary<string, object> _repositories = new();

    public IBaseRepository<T> Repository<T>() where T : class
    {
        var typeName = typeof(T).Name;

        return (IBaseRepository<T>)_repositories.GetOrAdd(typeName, _ =>
            new BaseRepository<T>(_context));
    }

    // Simplified shortcuts: They now point directly to the logic above
    public IBaseRepository<Document> Documents => Repository<Document>();
    public IBaseRepository<AuditLog> AuditLogs => Repository<AuditLog>();
    // 🌟 ADD THESE THREE LINES TO FIX THE CS0535 ERRORS:
    public IBaseRepository<Workflow> Workflows => Repository<Workflow>();
    public IBaseRepository<Category> Categories => Repository<Category>();
    public IBaseRepository<Status> Statuses => Repository<Status>();

    public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose()
    {
        // Let the DI container handle SrmDbContext disposal if registered as Scoped
        GC.SuppressFinalize(this);
    }
}