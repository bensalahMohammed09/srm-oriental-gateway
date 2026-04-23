using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedLookupDataAsync(SrmDbContext context)
        {
            // 1. Seed des Status (Indispensable pour l'ingestion)
            if (!await context.Statuses.AnyAsync())
            {
                var statuses = new List<Status>
            {
                new() { Id = Guid.NewGuid(), Name = "En attente d'indexation", Code = "TECH_TO_INDEX" },
                new() { Id = Guid.NewGuid(), Name = "En attente de validation métier", Code = "BUS_PENDING_VAL" },
                new() { Id = Guid.NewGuid(), Name = "Validé", Code = "APPROVED" },
                new() { Id = Guid.NewGuid(), Name = "Rejeté", Code = "REJECTED" }
            };

                await context.Statuses.AddRangeAsync(statuses);
            }

            // 2. Seed des Catégories (Indispensable pour la validation)
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
            {
                new() { Id = Guid.NewGuid(), Name = "Maintenance & Travaux", Description = "Factures liées aux infrastructures" },
                new() { Id = Guid.NewGuid(), Name = "Logistique & Transport", Description = "Frais de déplacement et flotte" },
                new() { Id = Guid.NewGuid(), Name = "Informatique & Télécom", Description = "Abonnements et matériels" }
            };

                await context.Categories.AddRangeAsync(categories);
            }

            await context.SaveChangesAsync();
        }
    }
}
