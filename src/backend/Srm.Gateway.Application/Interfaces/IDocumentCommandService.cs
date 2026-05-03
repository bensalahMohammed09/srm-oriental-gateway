using Microsoft.AspNetCore.Http;
using Srm.Gateway.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IDocumentCommandService
    {
        Task<Guid> IngestDocumentAsync(OcrIngestionRequest request);
        Task ConfirmIndexationAsync(Guid documentId, DocumentValidationRequest request);
        Task<Guid> CreateManualDocumentAsync(ManualUploadRequest request);
        Task<Guid> RecoverFailedDocumentAsync(ManualRecoveryRequest request);

        // Actions sur les fichiers physiques
        Task SaveFileToPendingAsync(IFormFile file);
        Task ArchiveDocumentFileAsync(Guid id);
    }
}