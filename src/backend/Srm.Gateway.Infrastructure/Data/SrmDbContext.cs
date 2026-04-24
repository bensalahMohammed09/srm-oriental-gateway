using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Infrastructure.Data;

public class SrmDbContext : IdentityDbContext<IdentityUser>
{
    public SrmDbContext(DbContextOptions<SrmDbContext> options) : base(options) { }

    public DbSet<Document> Documents { get; set; }
    // 🗑️ SUPPRIMÉ : public DbSet<OcrMetadata> Metadata { get; set; } (On n'en a plus besoin !)
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

        // --- Tes mappings existants ---
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<Status>().ToTable("statuses");
        modelBuilder.Entity<Workflow>().ToTable("workflows");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
        // 🗑️ SUPPRIMÉ : modelBuilder.Entity<OcrMetadata>().ToTable("ocr_metadata");

        // 🌟 NOUVEAU : Configuration avancée de l'entité Document (JSONB + GIN)
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasIndex(d => d.Reference).IsUnique();

            // 1. On indique que la propriété Metadata (le Dictionnaire) doit être stockée sous forme de JSON
            entity.OwnsOne(d => d.Metadata, builder =>
            {
                builder.ToJson();
            });

            // 2. On crée l'Index GIN pour que PostgreSQL puisse chercher instantanément dans le JSON
            // On utilise "Metadata" (string) car c'est une propriété de navigation ("Owned type") dans EF Core
            entity.HasIndex("Metadata").HasMethod("gin");
        });

        // --- Relations existantes (Inchangées) ---
        modelBuilder.Entity<Workflow>()
            .HasOne(w => w.Document)
            .WithMany(d => d.Workflows)
            .HasForeignKey(w => w.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Status>().HasIndex(s => s.Code).IsUnique();

        // 🗑️ SUPPRIMÉ : La relation HasOne/WithMany pour OcrMetadata a été retirée.
    }
}