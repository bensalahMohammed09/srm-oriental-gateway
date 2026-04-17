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

        // Configuration of mapping postgres

        modelBuilder.Entity<OcrMetadata>()
            .HasOne(m => m.Document)
            .WithMany(d => d.Metadata)
            .HasForeignKey(m => m.DocumentId);

        modelBuilder.Entity<Workflow>()
            .HasOne(w => w.Document)
            .WithMany(d => d.Workflows)
            .HasForeignKey(w => w.DocumentId);

        var entityTpes = modelBuilder.Model.GetEntityTypes();
        foreach(var entity in entityTpes)
        {
            modelBuilder.Entity(entity.ClrType)
                .Property("CreatedAt")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}