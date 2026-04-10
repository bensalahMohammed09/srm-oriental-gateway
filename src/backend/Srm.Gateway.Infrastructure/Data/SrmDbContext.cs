using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Infrastructure.Data;

public class  SrmDbContext(DbContextOptions<SrmDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<OcrMetadata> OcrMetadatas => Set<OcrMetadata>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}