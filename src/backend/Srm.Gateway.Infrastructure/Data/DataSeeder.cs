using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedLookupDataAsync(SrmDbContext context)
        {
            // Ensure database is ready and table structures are loaded before seeding
            await SeedStatusesAsync(context);
            await SeedCategoriesAsync(context);

            await context.SaveChangesAsync();
        }

        private static async Task SeedStatusesAsync(SrmDbContext context)
        {
            var defaultStatuses = new List<Status>
            {
                new() { Id = Guid.NewGuid(), Code = "TECH_TO_INDEX", Name = "Technique à Indexer" },
                new() { Id = Guid.NewGuid(), Code = "BUS_PENDING_VAL", Name = "En Attente de Validation" },
                new() { Id = Guid.NewGuid(), Code = "APPROVED", Name = "Approuvé" },
                new() { Id = Guid.NewGuid(), Code = "REJECTED", Name = "Rejeté" }
            };

            foreach (var defaultStatus in defaultStatuses)
            {
                // Prevent duplicate keys on multiple application restarts
                var exists = await context.Set<Status>()
                    .AnyAsync(s => s.Code == defaultStatus.Code);

                if (!exists)
                {
                    await context.Set<Status>().AddAsync(defaultStatus);
                }
            }
        }

        private static async Task SeedCategoriesAsync(SrmDbContext context)
        {
            var defaultCategories = new List<Category>
            {
                new() { Id = Guid.NewGuid(), Name = "Informatique" },
                new() { Id = Guid.NewGuid(), Name = "Télécom" },
                new() { Id = Guid.NewGuid(), Name = "Maintenance" },
                new() { Id = Guid.NewGuid(), Name = "Travaux" },
                new() { Id = Guid.NewGuid(), Name = "Prestations Générales" }
            };

            foreach (var defaultCategory in defaultCategories)
            {
                // Database-agnostic case-insensitive check (safe on SQLite, SQL Server, and PostgreSQL)
                var exists = await context.Set<Category>()
                    .AnyAsync(c => c.Name.ToUpper() == defaultCategory.Name.ToUpper());

                if (!exists)
                {
                    await context.Set<Category>().AddAsync(defaultCategory);
                }
            }
        }
    }
}