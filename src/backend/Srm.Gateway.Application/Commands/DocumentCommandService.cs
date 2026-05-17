using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Commands;

public class DocumentCommandService : IDocumentCommandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkflowService _workflowService;
    private readonly IFileStorageService _fileStorage;

    public DocumentCommandService(IUnitOfWork unitOfWork, IWorkflowService workflowService, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _workflowService = workflowService;
        _fileStorage = fileStorage;
    }

    public async Task ConfirmIndexationAsync(Guid documentId, DocumentValidationRequest request)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException("Document introuvable.");

        if (document.RowVersion != null && request.RowVersion != null && !document.RowVersion.SequenceEqual(request.RowVersion))
        {
            throw new InvalidOperationException("Conflit de modification : Ce document a déjà été modifié par un autre utilisateur. Veuillez rafraîchir la page.");
        }

        document.SupplierName = request.SupplierName;

        if (request.NewMetadata != null)
        {
            document.Metadata = request.NewMetadata.ToDictionary(
                kvp => kvp.Key,
                kvp => new DocumentFieldValue { Value = kvp.Value.Value?.ToString() ?? string.Empty, Confidence = kvp.Value.Confidence }
            );
        }

        document.CategoryId = request.CategoryId;
        document.Reference = request.Reference;
        document.TotalAmount = request.TotalAmount;

        var status = await _unitOfWork.Repository<Status>()
            .FindByCondition(s => s.Code == "BUS_PENDING_VAL")
            .FirstOrDefaultAsync() ?? throw new InvalidOperationException("Statut cible introuvable.");

        document.StatusId = status.Id;
        document.RowVersion = request.RowVersion ?? Array.Empty<byte>();

        try
        {
            _unitOfWork.Documents.Update(document);
            await _unitOfWork.CompleteAsync();

            await _workflowService.StartProcessAsync(document.Id, "Indexation BO terminée.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Conflit de modification détecté par la base de données.");
        }
    }

    public async Task<Guid> CreateManualDocumentAsync(ManualUploadRequest request)
    {
        var status = await _unitOfWork.Repository<Status>()
            .FindByCondition(s => s.Code == "BUS_PENDING_VAL")
            .FirstOrDefaultAsync() ?? throw new Exception("Statut cible introuvable.");

        var initialMetadata = request.Metadata?.ToDictionary(
            m => m.Key,
            m => new DocumentFieldValue { Value = m.Value?.ToString() ?? string.Empty, Confidence = 1.0 }
        ) ?? new Dictionary<string, DocumentFieldValue>();

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Reference = request.Reference,
            SupplierName = request.SupplierName ?? string.Empty,
            TotalAmount = request.TotalAmount,
            CategoryId = request.CategoryId,
            StatusId = status.Id,
            SourceFile = null,
            Metadata = initialMetadata
        };

        try
        {
            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.CompleteAsync();

            await _workflowService.StartProcessAsync(document.Id, "Saisie manuelle.");
            return document.Id;
        }
        catch (DbUpdateConcurrencyException) { throw new Exception("Erreur système lors de la création."); }
    }

    public async Task<Guid> IngestDocumentAsync(OcrIngestionRequest request)
    {
        var status = await _unitOfWork.Repository<Status>()
            .FindByCondition(s => s.Code == "TECH_TO_INDEX")
            .FirstOrDefaultAsync() ?? throw new InvalidOperationException("Le statut TECH_TO_INDEX n'existe pas en base de données.");

        var sourceFileMeta = request.Metadata?.Find(m => m.Key == "SourceFile");
        var sourceFileName = sourceFileMeta?.Value?.ToString();

        var filteredMetadata = request.Metadata?
            .Where(m => m.Key != "SourceFile" && m.Key != "ExtractionTimestamp")
            .ToDictionary(
                m => m.Key,
                m => new DocumentFieldValue { Value = m.Value?.ToString() ?? string.Empty, Confidence = m.Confidence }
            ) ?? new Dictionary<string, DocumentFieldValue>();

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Reference = request.Reference,
            SupplierName = request.SupplierName ?? string.Empty,
            TotalAmount = request.TotalAmount,
            SourceFile = sourceFileName,
            StatusId = status.Id,
            CreatedAt = DateTime.UtcNow,
            Metadata = filteredMetadata
        };

        await _unitOfWork.Documents.AddAsync(doc);
        await _unitOfWork.CompleteAsync();

        return doc.Id;
    }

    public async Task<Guid> RecoverFailedDocumentAsync(ManualRecoveryRequest request)
    {
        var status = await _unitOfWork.Repository<Status>()
            .FindByCondition(s => s.Code == "BUS_PENDING_VAL")
            .FirstOrDefaultAsync() ?? throw new InvalidOperationException("Statut cible introuvable."); ;

        // 🌟 FIX : On récupère les métadonnées envoyées par React
        var initialMetadata = request.Metadata?.ToDictionary(
            m => m.Key,
            m => new DocumentFieldValue { Value = m.Value.Value?.ToString() ?? string.Empty, Confidence = 1.0 }
        ) ?? new Dictionary<string, DocumentFieldValue>();

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Reference = request.Reference,
            SupplierName = request.SupplierName ?? string.Empty, // 🌟 FIX : Fournisseur
            TotalAmount = request.TotalAmount,
            CategoryId = request.CategoryId,     // 🌟 FIX : Catégorie (Essentiel pour le workflow !)
            StatusId = status.Id,
            SourceFile = request.FileName,
            Metadata = initialMetadata           // 🌟 FIX : Métadonnées
        };

        await _unitOfWork.Documents.AddAsync(document);

        try
        {
            await _unitOfWork.CompleteAsync();

            await _fileStorage.MoveFileAsync(request.FileName, StorageFolder.Failed, StorageFolder.Processed);
            await _workflowService.StartProcessAsync(document.Id, "Récupération manuelle suite à un échec OCR.");

            return document.Id;
        }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Conflit de récupération."); }
    }

    public async Task ArchiveDocumentFileAsync(Guid id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id) ?? throw new KeyNotFoundException();
        var status = await _unitOfWork.Repository<Status>().GetByIdAsync(document.StatusId);

        if (status == null || status.Code != "BUS_APPROVED")
        {
            throw new InvalidOperationException("Violation métier : Le fichier physique ne peut être archivé que si le document a été approuvé.");
        }

        if (!string.IsNullOrEmpty(document.SourceFile))
        {
            await _fileStorage.MoveFileAsync(document.SourceFile, StorageFolder.Processed, StorageFolder.Archived);
        }
    }

    public Task SaveFileToPendingAsync(IFormFile file)
    {
        return _fileStorage.SaveFileAsync(file, file.FileName, StorageFolder.Pending);
    }
}