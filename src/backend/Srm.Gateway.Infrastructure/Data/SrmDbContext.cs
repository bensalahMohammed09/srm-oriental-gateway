using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Domain.Entities;
using System;

namespace Srm.Gateway.Infrastructure.Data;

public class SrmDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public SrmDbContext(DbContextOptions<SrmDbContext> options) : base(options) { }

    public DbSet<Document> Documents { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 🌟 LE FIX MAGIQUE POUR POSTGRESQL QUE TU AVAIS OUBLIÉ 🌟
        // Force PostgreSQL à mettre une valeur par défaut pour TOUTES les colonnes RowVersion
        // Cela empêche le crash 23502 (NOT NULL constraint) lors du DataSeeder
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rowVersionProp = entityType.FindProperty("RowVersion");
            if (rowVersionProp != null && rowVersionProp.ClrType == typeof(byte[]))
            {
                rowVersionProp.SetDefaultValueSql("'\\x0000000000000000'");
            }
        }

        // Configuration avancée de l'entité Document (JSONB + GIN)
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasIndex(d => d.Reference).IsUnique();

            // 1. On indique que la propriété Metadata (le Dictionnaire) doit être stockée sous forme de JSON
            entity.Property(d => d.Metadata)
                   .HasColumnType("jsonb");

            // 2. Configuration stricte du jeton de concurrence
            entity.Property(d => d.RowVersion).IsRowVersion();

            // 3. On crée l'Index GIN pour que PostgreSQL puisse chercher instantanément dans le JSON
            entity.HasIndex("Metadata").HasMethod("gin");
        });

        // --- Relations existantes ---
        modelBuilder.Entity<Workflow>()
            .HasOne(w => w.Document)
            .WithMany(d => d.Workflows)
            .HasForeignKey(w => w.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Status>().HasIndex(s => s.Code).IsUnique();
    }
}