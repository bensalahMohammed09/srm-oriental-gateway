using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Infrastructure.Repositories
{
    public class UnitOfWork(SrmDbContext context) : IUnitOfWork
    {
        private readonly SrmDbContext _context = context;

        private IBaseRepository<Document>? _documents;
        public IBaseRepository<Document> Documents => _documents ??= new BaseRepository<Document>(_context);

        private IBaseRepository<OcrMetadata>? _metadata;
        public IBaseRepository<OcrMetadata> Metadata => _metadata ??= new BaseRepository<OcrMetadata>(_context);

        private IBaseRepository<Workflow>? _workflows;
        public IBaseRepository<Workflow> Workflows => _workflows ??= new BaseRepository<Workflow>(_context);

        private IBaseRepository<User>? _users;
        public IBaseRepository<User> Users => _users ??= new BaseRepository<User>(_context);

        private IBaseRepository<Role>? _roles;
        public IBaseRepository<Role> Roles => _roles ??= new BaseRepository<Role>(_context);

        private IBaseRepository<Category>? _categories;
        public IBaseRepository<Category> Categories => _categories ??= new BaseRepository<Category>(_context);

        private IBaseRepository<Status>? _statuses;
        public IBaseRepository<Status> Statuses => _statuses ??= new BaseRepository<Status>(_context);

        private IBaseRepository<AuditLog>? _auditLogs;
        public IBaseRepository<AuditLog> AuditLogs => _auditLogs ??= new BaseRepository<AuditLog>(_context);

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
