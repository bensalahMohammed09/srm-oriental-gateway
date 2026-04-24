using Microsoft.AspNetCore.Http;
using Srm.Gateway.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IDocumentService
    {
        // Ingestion technique (OCR)
        Task<Guid> IngestDocumentAsync(OcrIngestionRequest request);

        // Validation métier (Agent BO)
        Task ConfirmIndexationAsync(Guid documentId, DocumentValidationRequest request);

        // Listes et Recherches
        Task<IEnumerable<DocumentResponse>> GetPendingIndexationAsync();
        Task<IEnumerable<DocumentResponse>> SearchDocumentsAsync(string? query, string? status);
        Task<DocumentResponse?> GetByIdAsync(Guid id);

        // Gestion des fichiers (Upload / View)
        Task SaveFileToPendingAsync(IFormFile file);
        Task<FileDownloadDto> GetFileForViewAsync(Guid documentId);

        // Récupération manuelle et Saisie
        Task<Guid> RecoverFailedDocumentAsync(ManualRecoveryRequest request);
        Task<IEnumerable<FailedFileResponse>> GetFailedFilesAsync();
        Task<FileDownloadDto> GetFailedDocumentFileAsync(string fileName);
        Task<Guid> CreateManualDocumentAsync(ManualUploadRequest request);

        // Maintenance SRE
        Task ArchiveDocumentFileAsync(Guid documentId);

        Task<DocumentDetailsResponse?> GetDocumentDetailsAsync(Guid id);
    }
}