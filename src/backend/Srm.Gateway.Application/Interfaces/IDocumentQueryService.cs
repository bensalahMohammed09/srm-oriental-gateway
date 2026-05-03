using Srm.Gateway.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IDocumentQueryService
    {
        // 🌟 Ajout des paramètres de pagination (avec des valeurs par défaut)
        Task<PagedResult<DocumentResponse>> SearchDocumentsAsync(string? query, string? status, int pageNumber = 1, int pageSize = 10);
        Task<PagedResult<DocumentResponse>> GetPendingIndexationAsync(int pageNumber = 1, int pageSize = 10);

        Task<DocumentResponse?> GetByIdAsync(Guid id);
        Task<DocumentIndexationResponse?> GetDocumentDetailsAsync(Guid id);

        // Fichiers (Lecture seule)
        Task<IEnumerable<FailedFileResponse>> GetFailedFilesAsync();
        Task<FileDownloadDto> GetFileForViewAsync(Guid documentId);
        Task<FileDownloadDto> GetFailedDocumentFileAsync(string fileName);
    }
}