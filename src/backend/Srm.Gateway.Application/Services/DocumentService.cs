using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly string _uploadPath = "/app/uploads/pending";
        private readonly string _processedPath = "/app/uploads/processed";
        private readonly string _archivedPath = "/app/upload/archived";

        public DocumentService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        public async Task<Guid> IngestDocumentAsync(OcrIngestionRequest request)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Reference = request.Reference,
                SupplierName = request.SupplierName ?? "",
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
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
                           ?? throw new KeyNotFoundException("Document introuvable.");

            // 1. On récupère l'ID de l'agent qui valide
            var currentUserId = GetCurrentUserId();

            // 2. On met à jour le document
            document.CategoryId = request.CategoryId;
            document.Reference = request.Reference;
            document.TotalAmount = request.TotalAmount;

            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            document.StatusId = statuses.First(s => s.Code == "BUS_PENDING_VAL").Id;

            // 3. On crée l'entrée de Workflow (BPMN Logic)
            var workflowEntry = new Workflow
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                StepName = "Indexation initiale",
                CurrentStatus = "VALIDATED_BY_BO",
                ValidatedByUserId = currentUserId, // 🛡️ C'est ici que l'ID de l'admin/agent est stocké
                ValidatedAt = DateTime.UtcNow,
                Comment = "Indexation confirmée manuellement par l'agent."
            };

            await _unitOfWork.Workflows.AddAsync(workflowEntry);

            // Mise à jour des métadonnées (ton code existant...)
            if (request.MetadataCorrections != null) { /* ... */ }

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

        ///<summary>
        ///Step 1 : Save the files to trigger the OCR Worker.
        ///</summary>
        public async Task SaveFileToPendingAsync(IFormFile file)
        {
            if(!Directory.Exists(_uploadPath)) Directory.CreateDirectory(_uploadPath);

            var physicalPath = Path.Combine(_uploadPath, file.FileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }

        public async Task<(Stream stream,string contentType, string fileName)> GetDocumentFileAsync(Guid id)
        {
            // 1. find the filename in the metadata (The link created by the Python Worker)
            var metadata = await _unitOfWork.Metadata.GetAllAsync();
            var sourceFile = metadata.FirstOrDefault(m => m.DocumentId == id && m.Key == "SourceFile");

            if (sourceFile == null) throw new FileNotFoundException("Metadata for source file not found in database");

            var fileName = sourceFile.Value;
            var fullPath = Path.Combine(_processedPath, fileName);

            if (!File.Exists(fullPath)) throw new FileNotFoundException($"Physical file {fileName} not found in processed folder.");

            var stream = new FileStream(fullPath, FileMode.Open,FileAccess.Read);
            var contentType = GetMimeType(fileName);

           return (stream, contentType, fileName);
        }
        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream" // Si ça retourne ça, ça télécharge.
            };
        }

        public async Task<Guid> RecoverFailedDocumentAsync(ManualRecoveryRequest request)
        {
            var failedPath = Path.Combine("/app/uploads/failed", request.FileName);
            var processedPath = Path.Combine("/app/uploads/processed", request.FileName);

            var isDuplicate = await _unitOfWork.Documents.AnyAsync(d => d.Reference == request.Reference);
            if (isDuplicate)
                throw new InvalidOperationException($"Une facture avec la référence '{request.Reference}' existe déjà dans le système.");

            // 1. Vérifier que le fichier existe bien dans le dossier d'erreur
            if (!File.Exists(failedPath))
                throw new FileNotFoundException($"Le fichier {request.FileName} n'existe pas dans le dossier d'échec.");

            // 2. Récupérer l'ID du statut "Validation Métier en attente"
            var statuses = await _unitOfWork.Statuses.GetAllAsync(); // Idéalement, utilise une méthode GetByCode
            var pendingValStatus = statuses.FirstOrDefault(s => s.Code == "BUS_PENDING_VAL")
                                   ?? throw new Exception("Statut BUS_PENDING_VAL introuvable.");

            // 3. Créer le Document
            var document = new Document
            {
                Reference = request.Reference,
                TotalAmount = request.TotalAmount,
                StatusId = pendingValStatus.Id,
                CreatedAt = DateTime.UtcNow // <-- On garde la bonne pratique vue en Phase 2 !
            };

            await _unitOfWork.Documents.AddAsync(document);

            // 4. Créer la métadonnée technique pour le File Bridge
            var sourceFileMeta = new OcrMetadata
            {
                DocumentId = document.Id,
                Key = "SourceFile",
                Value = request.FileName,
                Confidence = 1.0, // Confiance à 100% car c'est un humain qui valide
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Metadata.AddAsync(sourceFileMeta);

            // 5. Sauvegarder en base (Transaction SQL)
            await _unitOfWork.CompleteAsync();

            // 6. Déplacer le fichier physiquement (Transaction Disque)
            // On le fait APRÈS le CompleteAsync pour ne pas déplacer le fichier si la DB plante
            File.Move(failedPath, processedPath, overwrite: true);

            return document.Id;
        }

        public Task<IEnumerable<object>> GetFailedFilesAsync()
        {
            var failedPath = "/app/uploads/failed";

            // Sécurité : si le dossier n'existe pas encore, on renvoie une liste vide
            if (!Directory.Exists(failedPath))
                return Task.FromResult<IEnumerable<object>>(new List<object>());

            var files = Directory.GetFiles(failedPath)
                                 .Select(f => new
                                 {
                                     FileName = Path.GetFileName(f),
                                     // On convertit la taille en Ko pour l'affichage React
                                     SizeKb = Math.Round(new FileInfo(f).Length / 1024.0, 2),
                                     CreationTime = File.GetCreationTime(f)
                                 })
                                 .OrderBy(f => f.CreationTime); // Les plus anciens en premier

            return Task.FromResult<IEnumerable<object>>(files);
        }

        public Task<(Stream stream, string contentType)> GetFailedDocumentFileAsync(string fileName)
        {
            var failedPath = "/app/uploads/failed";
            var fullPath = Path.Combine(failedPath, fileName);

            // 🛡️ SÉCURITÉ IMPORTANTE (Directory Traversal)
            // On s'assure que personne n'essaie d'envoyer un fileName comme "../../etc/passwd"
            var normalizedFullPath = Path.GetFullPath(fullPath);
            if (!normalizedFullPath.StartsWith(failedPath))
                throw new UnauthorizedAccessException("Tentative d'accès non autorisée en dehors du dossier.");

            // Vérification de l'existence
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Le fichier {fileName} n'existe pas dans le dossier d'échec.");

            // On ouvre le flux
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

            // On réutilise ta méthode GetMimeType existante !
            var contentType = GetMimeType(fileName);

            return Task.FromResult<(Stream, string)>((stream, contentType));
        }

        public async Task<Guid> CreateManualDocumentAsync(ManualUploadRequest request)
        {
            var currentUserId = GetCurrentUserId();

            // 1. Sauvegarde physique du fichier
            var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
            var filePath = Path.Combine(_uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            // 2. Création du document (Statut direct : BUS_PENDING_VAL)
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var pendingStatus = statuses.First(s => s.Code == "BUS_PENDING_VAL");

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Reference = request.Reference,
                SupplierName = request.SupplierName,
                TotalAmount = request.TotalAmount,
                CategoryId = request.CategoryId,
                StatusId = pendingStatus.Id,
                CreatedAt = DateTime.UtcNow
            };

            // 3. Liaison avec le fichier via Metadata
            document.Metadata.Add(new OcrMetadata
            {
                Id = Guid.NewGuid(),
                Key = "SourceFile",
                Value = fileName
            });

            // 4. Workflow : Création manuelle
            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                StepName = "Saisie manuelle BO",
                CurrentStatus = "CREATED_BY_AGENT",
                ValidatedByUserId = currentUserId,
                ValidatedAt = DateTime.UtcNow,
                Comment = "Document ajouté manuellement par l'agent sans passer par l'OCR."
            };

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.Workflows.AddAsync(workflow);
            await _unitOfWork.CompleteAsync();

            return document.Id;
        }

        public async Task ArchiveDocumentFileAsync(Guid documentId)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
            if (document == null) return;

            var fileNameMeta = document.Metadata.FirstOrDefault(m => m.Key == "file_name");
            if (fileNameMeta == null) return;

            var sourcePath = Path.Combine(_processedPath, fileNameMeta.Value);
            var destinationPath = Path.Combine(_archivedPath, fileNameMeta.Value);

            try
            {
                if (File.Exists(sourcePath))
                {
                    // S'assurer que le dossier de destination existe
                    if (!Directory.Exists(_archivedPath)) Directory.CreateDirectory(_archivedPath);

                    // Déplacement physique du fichier (Archive SRE)
                    File.Move(sourcePath, destinationPath, overwrite: true);

                    // Optionnel : Mettre à jour la métadonnée pour pointer vers le nouveau chemin
                    fileNameMeta.Value = fileNameMeta.Value; // Le nom reste le même, mais le dossier change
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex) 
            {
                throw new Exception($"[SRE-ERR] Échec de l'archivage physique : {ex.Message}");
            }
            
        }
        public async Task<FileDownloadDto> GetFileForViewAsync(Guid documentId)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
                ?? throw new KeyNotFoundException("Document introuvable.");

            var fileName = document.Metadata.FirstOrDefault(m => m.Key == "SourceFile")?.Value
                ?? throw new FileNotFoundException("Aucun fichier associé.");

            string[] searchPaths = {
            Path.Combine(_archivedPath, fileName),
            Path.Combine(_processedPath, fileName),
            Path.Combine(_uploadPath, fileName)
        };

            string? finalPath = searchPaths.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException("Fichier physique introuvable.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(finalPath, out var contentType)) contentType = "application/octet-stream";

            return new FileDownloadDto(File.OpenRead(finalPath), contentType, fileName);
        }

        public async Task<IEnumerable<DocumentResponse>> SearchDocumentsAsync(string? query, string? status)
        {
            var docs = await _unitOfWork.Documents.GetAllAsync();

            return docs.Where(d =>
                (string.IsNullOrEmpty(query) || d.Reference.Contains(query, StringComparison.OrdinalIgnoreCase) || d.SupplierName.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(status) || d.Status.Code == status))
                .Select(d => new DocumentResponse(d.Id, d.Reference, d.Status.Name, d.Category?.Name, d.CreatedAt));
        }

    }
}
