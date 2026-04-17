using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<Domain.Entities.Document> Documents { get; }
        IBaseRepository<Domain.Entities.OcrMetadata> Metadata { get; }
        IBaseRepository<Domain.Entities.Workflow> Workflows { get; }
        IBaseRepository<Domain.Entities.User> Users { get; }
        IBaseRepository<Domain.Entities.Role> Roles { get; }
        IBaseRepository<Domain.Entities.Category> Categories { get; }
        IBaseRepository<Domain.Entities.Status> Statuses { get; }
        IBaseRepository<Domain.Entities.AuditLog> AuditLogs { get; }

        Task<int> CompleteAsync(); // Sauvegarde transactionnelle
    }
}
