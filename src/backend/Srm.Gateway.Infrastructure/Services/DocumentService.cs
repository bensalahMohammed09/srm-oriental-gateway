using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using Srm.Gateway.Infrastructure.Data;

namespace Srm.Gateway.Infrastructure.Services
{
    public class DocumentService(SrmDbContext srmDb) : IDocumentService
    {
        public async Task<Document> RegisterDocumentAsync(Document document)
        {
            srmDb.Documents.Add(document);

            var log = new AuditLog
            {
                EventType = "CREATION",
                Description = $"Nouveau document enregistré : {document.ReferenceNumber}"
            };

            srmDb.AuditLogs.Add(log);

            await srmDb.SaveChangesAsync();

            return document;
        }

        public async Task<bool> UpdateStatusAsync(int documentId, int newStatusId, int userId, string comment)
        {
            var doc = await srmDb.Documents.FindAsync(documentId);
            if (doc == null) return false;

            doc.StatusId = newStatusId;

            // Industrial Standard: Tracking workflow history
            var history = new Workflow
            {
                DocumentId = documentId,
                UserId = userId,
                ActionTaken = $"Status updated to {newStatusId}. Comment: {comment}",
                ActionDate = DateTime.UtcNow
            };

            srmDb.Workflows.Add(history);
            return await srmDb.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Document>> GetAllDocumentsAsync() =>
            await srmDb.Documents
            .Include(d => d.Category)
            .Include(d => d.Status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        public async Task<Document?> GetByReferenceAsync(string reference) =>
            await srmDb.Documents
            .Include(d => d.Category)
            .Include(d => d.Status)
            .FirstOrDefaultAsync(d => d.ReferenceNumber == reference);


    }
}
