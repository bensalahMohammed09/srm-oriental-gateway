using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Services
{
    public class DocumentService(IUnitOfWork unitOfWork) : IDocumentService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Guid> IngestDocumentAsync(OcrIngestionRequest request)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Reference = request.Reference,
                SupplierName = request.SupplierName,
                TotalAmount = request.TotalAmount ?? 0,
                StatusId = await GetStatusIdByCode("TECH_TO_INDEX") // Le document est "parqué" ici
            };

            await _unitOfWork.Documents.AddAsync(document);

            foreach (var metaDto in request.Metadata)
            {
                await _unitOfWork.Metadata.AddAsync(new OcrMetadata
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    Key = metaDto.Key,
                    Value = metaDto.Value,
                    Confidence = metaDto.Confidence
                });
            }

            await _unitOfWork.CompleteAsync();
            return document.Id;
        }

        public async Task ConfirmIndexationAsync(Guid documentId, DocumentValidationRequest request)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null) return;

            document.Reference = request.Reference;
            document.TotalAmount = request.TotalAmount;
            document.CategoryId = request.CategoryId;
            document.UpdatedAt = DateTime.UtcNow;
            document.StatusId = await GetStatusIdByCode("BUS_PENDING_APPROVAL");

            await _unitOfWork.CompleteAsync();
        }

        // Pour l'agent qui revient de sa pause : il voit ce qui est en attente
        public async Task<IEnumerable<DocumentResponse>> GetPendingIndexationAsync()
        {
            var statusId = await GetStatusIdByCode("TECH_TO_INDEX");
            var docs = await _unitOfWork.Documents.GetAllAsync();

            // On filtre en mémoire (Règle simple pour le PoC)
            return docs.Where(d => d.StatusId == statusId)
                       .Select(d => new DocumentResponse(
                           d.Id,
                           d.Reference,
                           "En attente d'indexation",
                           null,
                           d.CreatedAt));
        }

        public async Task<DocumentResponse?> GetByIdAsync(Guid id)
        {
            var d = await _unitOfWork.Documents.GetByIdAsync(id);
            if (d == null) return null;

            return new DocumentResponse(d.Id, d.Reference, "Status", null, d.CreatedAt);
        }

        private async Task<Guid> GetStatusIdByCode(string code)
        {
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            return statuses.FirstOrDefault(s => s.Code == code)?.Id ?? Guid.Empty;
        }
    }
}
