using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Infrastructure.Data;

public class SrmDbContext : DbContext
{
    public SrmDbContext(DbContextOptions<SrmDbContext> options) : base(options) { }

    public DbSet<Document> Documents { get; set; }
    public DbSet<OcrMetadata> Metadata { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- 1. Mapping des noms de tables (snake_case) ---
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<Status>().ToTable("statuses");
        modelBuilder.Entity<Document>().ToTable("documents");
        modelBuilder.Entity<OcrMetadata>().ToTable("ocr_metadata");
        modelBuilder.Entity<Workflow>().ToTable("workflows");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

        // --- 2. Configuration des relations (Foreign Keys) ---

        // Document -> Metadata (1:N)
        modelBuilder.Entity<OcrMetadata>()
            .HasOne(m => m.Document)
            .WithMany(d => d.Metadata)
            .HasForeignKey(m => m.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Document -> Workflow (1:N)
        modelBuilder.Entity<Workflow>()
            .HasOne(w => w.Document)
            .WithMany(d => d.Workflows)
            .HasForeignKey(w => w.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Role (N:1)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.SetNull);

        // --- 3. Index Uniques ---

        // Index unique sur la référence du document (Critique pour éviter les doublons de factures)
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.Reference)
            .IsUnique();

        // Index unique sur les codes pour éviter les doublons techniques
        modelBuilder.Entity<Status>().HasIndex(s => s.Code).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.Code).IsUnique();
    }

}