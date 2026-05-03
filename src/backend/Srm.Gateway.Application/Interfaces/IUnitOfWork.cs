using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // The Generic method handles everything dynamically
    IBaseRepository<T> Repository<T>() where T : class;

    // Shortcuts for commonly used entities
    IBaseRepository<Document> Documents { get; }
    IBaseRepository<AuditLog> AuditLogs { get; }

    IBaseRepository<Workflow> Workflows { get; }

    IBaseRepository<Category> Categories { get; }
    IBaseRepository<Status> Statuses { get; }


    Task<int> CompleteAsync();
}