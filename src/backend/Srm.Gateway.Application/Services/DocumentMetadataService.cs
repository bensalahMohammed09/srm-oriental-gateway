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
        // 🟢 Utilisation propre du Repository via l'Unit Of Work
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Le document avec l'ID {documentId} est introuvable.");

        return document.Metadata;
    }

    public async Task UpdateMetadataAsync(Guid documentId, Dictionary<string, DocumentFieldValue> newMetadata)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Le document avec l'ID {documentId} est introuvable.");

        // L'approche "Clear & Replace"
        document.Metadata = newMetadata;

        // 🟢 On valide la transaction via l'Unit Of Work (qui appellera SaveChangesAsync sous le capot)
        _unitOfWork.Documents.Update(document);
        await _unitOfWork.CompleteAsync();
    }
}