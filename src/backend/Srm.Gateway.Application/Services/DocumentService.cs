using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWorkflowService _workflowService;
        private readonly IFileStorageService _fileStorage;

        public DocumentService(IUnitOfWork unitOfWork, IWorkflowService workflowService, IFileStorageService fileStorage)
        {
            _unitOfWork = unitOfWork;
            _workflowService = workflowService;
            _fileStorage = fileStorage;
        }

        public async Task<FileDownloadDto> GetFileForViewAsync(Guid documentId)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
                ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");

            var fileName = document.SourceFile
                ?? throw new FileNotFoundException("Aucun fichier physique n'est attaché à ce document.");

            (Stream Stream, string ContentType) fileData;

            try { fileData = await _fileStorage.GetFileStreamAsync(fileName, StorageFolder.Processed); }
            catch
            {
                try { fileData = await _fileStorage.GetFileStreamAsync(fileName, StorageFolder.Pending); }
                catch { fileData = await _fileStorage.GetFileStreamAsync(fileName, StorageFolder.Archived); }
            }

            return new FileDownloadDto(fileData.Stream, fileData.ContentType, fileName);
        }

        public async Task ConfirmIndexationAsync(Guid documentId, DocumentValidationRequest request)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId) ?? throw new KeyNotFoundException();
            document.CategoryId = request.CategoryId;
            document.Reference = request.Reference;
            document.TotalAmount = request.TotalAmount;

            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            document.StatusId = statuses.First(s => s.Code == "BUS_PENDING_VAL").Id;

            try
            {
                _unitOfWork.Documents.Update(document);
                await _unitOfWork.CompleteAsync();
                await _workflowService.StartProcessAsync(document.Id, "Indexation BO terminée.");
            }
            catch (DbUpdateConcurrencyException) { throw new Exception("Conflit de modification : rafraîchissez la page."); }
        }

        public async Task<Guid> CreateManualDocumentAsync(ManualUploadRequest request)
        {
            var statuses = await _unitOfWork.Statuses.GetAllAsync();

            // 🌟 MAPPING DES MÉTADONNÉES AVEC CONFIDENCE À 1.0 (CAR C'EST UN HUMAIN QUI SAISIT)
            var initialMetadata = request.Metadata?.ToDictionary(
                m => m.Key,
                m => new DocumentFieldValue { Value = m.Value.Value, Confidence = 1.0 }
            ) ?? new Dictionary<string, DocumentFieldValue>();

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Reference = request.Reference,
                SupplierName = request.SupplierName,
                TotalAmount = request.TotalAmount,
                CategoryId = request.CategoryId,
                StatusId = statuses.First(s => s.Code == "BUS_PENDING_VAL").Id,
                SourceFile = null, // PAS DE FICHIER
                Metadata = initialMetadata // 🌟 LE JSON EST MAINTENANT SAUVEGARDÉ EN BASE
            };

            try
            {
                await _unitOfWork.Documents.AddAsync(document);
                await _unitOfWork.CompleteAsync();
                await _workflowService.StartProcessAsync(document.Id, "Saisie manuelle.");
                return document.Id;
            }
            catch (DbUpdateConcurrencyException) { throw new Exception("Erreur de concurrence lors de la création."); }
        }

        public async Task<Guid> RecoverFailedDocumentAsync(ManualRecoveryRequest request)
        {
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Reference = request.Reference,
                TotalAmount = request.TotalAmount,
                StatusId = statuses.First(s => s.Code == "BUS_PENDING_VAL").Id,
                SourceFile = request.FileName,
                Metadata = new Dictionary<string, DocumentFieldValue>()
            };

            await _unitOfWork.Documents.AddAsync(document);

            try
            {
                await _unitOfWork.CompleteAsync();
                await _fileStorage.MoveFileAsync(request.FileName, StorageFolder.Failed, StorageFolder.Processed);
                await _workflowService.StartProcessAsync(document.Id, "Récupération OCR.");
                return document.Id;
            }
            catch (DbUpdateConcurrencyException) { throw new Exception("Conflit de récupération."); }
        }

        public async Task<IEnumerable<DocumentResponse>> GetPendingIndexationAsync()
        {
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var statusId = statuses.First(s => s.Code == "TECH_TO_INDEX").Id;
            var docs = await _unitOfWork.Documents.GetAllAsync();
            return docs.Where(d => d.StatusId == statusId).Select(d => new DocumentResponse(d.Id, d.Reference, "À Indexer", null, d.CreatedAt));
        }

        public async Task<Guid> IngestDocumentAsync(OcrIngestionRequest request)
        {
            var statuses = await _unitOfWork.Statuses.GetAllAsync();

            var initialMetadata = request.Metadata?.ToDictionary(
                m => m.Key,
                m => new DocumentFieldValue { Value = m.Value, Confidence = m.Confidence }
            ) ?? new Dictionary<string, DocumentFieldValue>();

            var doc = new Document
            {
                Id = Guid.NewGuid(),
                Reference = request.Reference,
                StatusId = statuses.First(s => s.Code == "TECH_TO_INDEX").Id,
                Metadata = initialMetadata
            };

            await _unitOfWork.Documents.AddAsync(doc);
            await _unitOfWork.CompleteAsync();
            return doc.Id;
        }

        public Task<IEnumerable<FailedFileResponse>> GetFailedFilesAsync()
        {
            var files = _fileStorage.GetFilesDetails(StorageFolder.Failed)
                .Select(f => new FailedFileResponse(f.FileName, f.SizeKb, f.CreationTime));

            return Task.FromResult(files);
        }

        public async Task<DocumentResponse?> GetByIdAsync(Guid id)
        {
            var d = await _unitOfWork.Documents.GetByIdAsync(id);
            return d == null ? null : new DocumentResponse(d.Id, d.Reference, d.Status.Name, d.Category?.Name, d.CreatedAt);
        }

        public async Task<DocumentDetailsResponse?> GetDocumentDetailsAsync(Guid id)
        {
            var d = await _unitOfWork.Documents.GetByIdAsync(id);
            if (d == null) return null;

            var metadataDtos = d.Metadata?.ToDictionary(
                k => k.Key,
                v => new MetadataValueDto(v.Value.Value, v.Value.Confidence)
            ) ?? new Dictionary<string, MetadataValueDto>();

            return new DocumentDetailsResponse(
                d.Id,
                d.Reference,
                d.Status.Name,
                d.Category?.Name,
                d.TotalAmount,
                d.SourceFile,
                metadataDtos,
                d.CreatedAt
            );
        }

        public async Task<IEnumerable<DocumentResponse>> SearchDocumentsAsync(string? q, string? s)
        {
            var docs = await _unitOfWork.Documents.GetAllAsync();
            return docs.Where(d => (string.IsNullOrEmpty(q) || d.Reference.Contains(q)) && (string.IsNullOrEmpty(s) || d.Status.Code == s)).Select(d => new DocumentResponse(d.Id, d.Reference, d.Status.Name, d.Category?.Name, d.CreatedAt));
        }

        public async Task ArchiveDocumentFileAsync(Guid id)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(id) ?? throw new KeyNotFoundException();

            var status = await _unitOfWork.Statuses.GetByIdAsync(document.StatusId);
            if (status == null || status.Code != "BUS_APPROVED")
            {
                throw new InvalidOperationException("Violation métier : Le fichier physique ne peut être archivé que si le document a été approuvé par tous les workflows.");
            }

            if (!string.IsNullOrEmpty(document.SourceFile))
            {
                await _fileStorage.MoveFileAsync(document.SourceFile, StorageFolder.Processed, StorageFolder.Archived);
            }
        }

        public async Task<FileDownloadDto> GetFailedDocumentFileAsync(string fn)
        {
            var data = await _fileStorage.GetFileStreamAsync(fn, StorageFolder.Failed);
            return new FileDownloadDto(data.Stream, data.ContentType, fn);
        }

        public Task SaveFileToPendingAsync(IFormFile f) => _fileStorage.SaveFileAsync(f, f.FileName, StorageFolder.Pending);
    }
}