using Srm.Gateway.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IDocumentService
    {
        // Flux Machine (OCR)
        Task<Guid> IngestDocumentAsync(OcrIngestionRequest request);

        // Flux Humain (Indexation)
        Task ConfirmIndexationAsync(Guid documentId, DocumentValidationRequest request);

        // Flux Récupération (Pour gérer les interruptions de l'agent)
        Task<IEnumerable<DocumentResponse>> GetPendingIndexationAsync();

        Task<DocumentResponse?> GetByIdAsync(Guid id);
    }
}
