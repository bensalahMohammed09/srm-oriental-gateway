using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
    public class WorkflowService : IWorkflowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IDocumentService _documentService;
        // 🗺️ Configuration des chemins BPMN (Dynamic Routing)
        // On définit quel rôle doit valider après le BO pour chaque catégorie
        private readonly Dictionary<string, string[]> _workflowPaths = new()
    {
        { "INFORMATIQUE_&_TÉLÉCOM", new[] { "ROLE_BO", "ROLE_TECH", "ROLE_FINANCE" } },
        { "MAINTENANCE_&_TRAVAUX", new[] { "ROLE_BO", "ROLE_MAINTENANCE", "ROLE_DIRECTOR", "ROLE_FINANCE" } },
        { "LOGISTIQUE_&_TRANSPORT", new[] { "ROLE_BO", "ROLE_LOGISTIQUE", "ROLE_FINANCE" } }
    };

        public WorkflowService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            RoleManager<IdentityRole> roleManager,IDocumentService documentService)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _roleManager = roleManager;
            _documentService = documentService;
        }

        public async Task<IEnumerable<Document>> GetMyPendingTasksAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return Enumerable.Empty<Document>();

            var userRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            // Correction : Utilisation de GetAllAsync + filtrage en mémoire
            var allDocuments = await _unitOfWork.Documents.GetAllAsync();

            return allDocuments.Where(d =>
            {
                var lastWorkflow = d.Workflows.OrderByDescending(w => w.ValidatedAt).FirstOrDefault();
                // Le document m'appartient si le rôle assigné à la dernière étape correspond à l'un de mes rôles
                return lastWorkflow != null && userRoles.Contains(lastWorkflow.AssignedRole?.Name ?? "");
            });
        }

        public async Task ApproveDocumentAsync(Guid documentId, string? comment)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
                ?? throw new KeyNotFoundException("Document introuvable");

            // 1. Déterminer le chemin basé sur la catégorie
            var categoryName = document.Category.Name.ToUpper().Replace(" ", "_");
            if (!_workflowPaths.TryGetValue(categoryName, out var path))
            {
                path = new[] { "ROLE_BO", "ROLE_FINANCE" }; // Chemin par défaut si catégorie inconnue
            }

            // 2. Trouver la position actuelle dans le chemin
            var lastWorkflow = document.Workflows.OrderByDescending(w => w.ValidatedAt).FirstOrDefault();
            var currentRoleName = lastWorkflow?.AssignedRole?.Name ?? "ROLE_BO";

            int currentIndex = Array.IndexOf(path, currentRoleName);
            int nextIndex = currentIndex + 1;

            // 3. Calculer la suite
            if (nextIndex >= path.Length)
            {
                await FinalizeApproval(document, comment);
            }
            else
            {
                await MoveToNextStep(document, path[nextIndex], comment);
            }
        }

        public async Task RejectDocumentAsync(Guid documentId, string reason)
        {
            var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
                ?? throw new KeyNotFoundException();

            var currentUserId = GetUserId();
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var boRole = await _roleManager.FindByNameAsync("ROLE_BO");

            document.StatusId = statuses.First(s => s.Code == "REJECTED").Id;

            var workflowEntry = new Workflow
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                StepName = "Rejet pour correction",
                CurrentStatus = "REJECTED",
                AssignedRoleId = boRole?.Id ?? "",
                ValidatedByUserId = currentUserId,
                ValidatedAt = DateTime.UtcNow,
                Comment = reason
            };

            await _unitOfWork.Workflows.AddAsync(workflowEntry);
            await _unitOfWork.CompleteAsync();
        }

        // --- 🛠️ Méthodes Privées de Transition ---

        private async Task MoveToNextStep(Document document, string nextRoleName, string? comment)
        {
            var currentUserId = GetUserId();
            var statuses = await _unitOfWork.Statuses.GetAllAsync();
            var nextRole = await _roleManager.FindByNameAsync(nextRoleName);

            // On passe au statut "En attente" pour l'étape suivante
            document.StatusId = statuses.First(s => s.Code == "BUS_PENDING_VAL").Id;

            var workflowEntry = new Workflow
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                StepName = $"Transmission à {nextRoleName}",
                CurrentStatus = "BUS_PENDING_VAL",
                AssignedRoleId = nextRole?.Id ?? "",
                ValidatedByUserId = currentUserId,
                ValidatedAt = DateTime.UtcNow,
                Comment = comment
            };

            await _unitOfWork.Workflows.AddAsync(workflowEntry);
            await _unitOfWork.CompleteAsync();
        }

        private async Task FinalizeApproval(Document document, string? comment)
        {
            var currentUserId = GetUserId();
            var statuses = await _unitOfWork.Statuses.GetAllAsync();

            document.StatusId = statuses.First(s => s.Code == "APPROVED").Id;

            var workflowEntry = new Workflow
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                StepName = "Clôture du Workflow",
                CurrentStatus = "APPROVED",
                AssignedRoleId = "",
                ValidatedByUserId = currentUserId,
                ValidatedAt = DateTime.UtcNow,
                Comment = comment ?? "Approbation finale et mise en archive."
            };

            await _unitOfWork.Workflows.AddAsync(workflowEntry);
            await _unitOfWork.CompleteAsync();

            // 🚀 DÉCLENCHEMENT SRE : On déplace le fichier vers l'archive
            await _documentService.ArchiveDocumentFileAsync(document.Id);
        }

        private string? GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

       
    }
}
