using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Infrastructure.Data;

// On hérite maintenant de IdentityDbContext pour intégrer les tables AspNetUsers, AspNetRoles, etc.
public class SrmDbContext : IdentityDbContext<IdentityUser>
{
    public SrmDbContext(DbContextOptions<SrmDbContext> options) : base(options) { }

    public DbSet<Document> Documents { get; set; }
    public DbSet<OcrMetadata> Metadata { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Mapping Identity en snake_case (Standard SRE) ---
        modelBuilder.Entity<IdentityUser>().ToTable("identity_users");
        modelBuilder.Entity<IdentityRole>().ToTable("identity_roles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("identity_user_roles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("identity_user_claims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("identity_user_logins");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("identity_role_claims");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("identity_user_tokens");

        // --- Tes mappings existants (Inchangés) ---
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<Status>().ToTable("statuses");
        modelBuilder.Entity<Document>().ToTable("documents");
        modelBuilder.Entity<OcrMetadata>().ToTable("ocr_metadata");
        modelBuilder.Entity<Workflow>().ToTable("workflows");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

        modelBuilder.Entity<OcrMetadata>()
            .HasOne(m => m.Document)
            .WithMany(d => d.Metadata)
            .HasForeignKey(m => m.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Workflow>()
            .HasOne(w => w.Document)
            .WithMany(d => d.Workflows)
            .HasForeignKey(w => w.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>().HasIndex(d => d.Reference).IsUnique();
        modelBuilder.Entity<Status>().HasIndex(s => s.Code).IsUnique();
    }
}