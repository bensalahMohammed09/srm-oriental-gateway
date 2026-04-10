using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Seed(SrmDbContext srmDb)
        {
            srmDb.Database.EnsureCreated();

            if (srmDb.Documents.Any()) return; // Db already seeded

            var sampleDocs = new[] {
                new Document {
                    ReferenceNumber = "SRM-OUJ-2026-001",
                    CategoryId = 1, // Facture
                    StatusId = 1,    // En attente
                    CreatedAt = DateTime.UtcNow
                },
                new Document {
                    ReferenceNumber = "SRM-BER-2026-042",
                    CategoryId = 2, // Courrier entrant
                    StatusId = 2,    // En cours
                    CreatedAt = DateTime.UtcNow
                }
            };

            srmDb.Documents.AddRange(sampleDocs);
            srmDb.SaveChanges();
        }
    }
}
