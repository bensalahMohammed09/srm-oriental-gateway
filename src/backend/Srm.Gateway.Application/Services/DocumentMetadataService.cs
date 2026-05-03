using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Application.Services;

public class DocumentMetadataService : IDocumentMetadataService
{
    private readonly IUnitOfWork _unitOfWork;

    public DocumentMetadataService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<string, DocumentFieldValue>?> GetMetadataAsync(Guid documentId)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Le document avec l'ID {documentId} est introuvable.");

        return document.Metadata;
    }

    public async Task UpdateMetadataAsync(Guid documentId, Dictionary<string, DocumentFieldValue> newMetadata)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Le document avec l'ID {documentId} est introuvable.");

        document.Metadata = newMetadata;

        _unitOfWork.Documents.Update(document);
        await _unitOfWork.CompleteAsync();
    }
}