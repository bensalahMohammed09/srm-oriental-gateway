using Microsoft.AspNetCore.Http;
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

        // New method for handling the physical upload
        Task SaveFileToPendingAsync(IFormFile file);
        // Fetches the physical file stream and its MIME type from the processed storage.
        Task<(Stream stream, string contentType, string fileName)> GetDocumentFileAsync(Guid id);

        /// <summary>
        /// Récupère un fichier en échec, l'insère en base avec les données saisies manuellement, et le déplace vers processed.
        /// </summary>
        Task<Guid> RecoverFailedDocumentAsync(ManualRecoveryRequest request);

        Task<IEnumerable<object>> GetFailedFilesAsync();

        Task<(Stream stream, string contentType)> GetFailedDocumentFileAsync(string fileName);
    }
}
