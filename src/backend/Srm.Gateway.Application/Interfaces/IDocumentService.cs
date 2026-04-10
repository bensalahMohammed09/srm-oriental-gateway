using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> RegisterDocumentAsync(Document document);

        Task<Document?> GetByReferenceAsync(string reference);

        Task<bool> UpdateStatusAsync(int documentId, int newStatusId, int userId, string comment );

        Task<IEnumerable<Document>> GetAllDocumentsAsync();
    }
}
