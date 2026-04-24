using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IFileStorageService _fileStorage;

    private readonly Dictionary<string, string[]> _workflowPaths = new()
    {
        { "INFORMATIQUE_&_TÉLÉCOM", new[] { "ROLE_BO", "ROLE_TECH", "ROLE_FINANCE" } },
        { "MAINTENANCE_&_TRAVAUX", new[] { "ROLE_BO", "ROLE_MAINTENANCE", "ROLE_DIRECTOR", "ROLE_FINANCE" } }
    };

    public WorkflowService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, RoleManager<IdentityRole> roleManager, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _roleManager = roleManager;
        _fileStorage = fileStorage;
    }

    public async Task StartProcessAsync(Guid documentId, string? initialComment)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId) ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");
        var path = GetPathForCategory(document.Category?.Name);
        await MoveToNextStep(document, path[1], initialComment ?? "Lancement.");
    }

    public async Task ApproveStepAsync(Guid documentId, string? comment)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId) ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");
        var path = GetPathForCategory(document.Category?.Name);
        var lastRole = document.Workflows.OrderByDescending(w => w.ValidatedAt).FirstOrDefault()?.AssignedRole?.Name ?? "ROLE_BO";
        int nextIndex = Array.IndexOf(path, lastRole) + 1;

        if (nextIndex >= path.Length) await FinalizeDocument(document, comment);
        else await MoveToNextStep(document, path[nextIndex], comment);
    }

    public async Task RejectStepAsync(Guid documentId, string reason)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId) ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");
        var statuses = await _unitOfWork.Statuses.GetAllAsync();
        var rejectedStatus = statuses.FirstOrDefault(s => s.Code == "REJECTED") ?? throw new InvalidOperationException("Statut REJECTED introuvable en base.");

        document.StatusId = rejectedStatus.Id;

        await _unitOfWork.Workflows.AddAsync(new Workflow
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            StepName = "Rejet",
            CurrentStatus = "REJECTED",
            AssignedRoleId = (await _roleManager.FindByNameAsync("ROLE_BO"))?.Id ?? "",
            ValidatedByUserId = GetUserId(),
            ValidatedAt = DateTime.UtcNow,
            Comment = reason
        });

        try
        {
            await _unitOfWork.CompleteAsync();

            // 🌟 SRE FAIL-SAFE : Nettoyage du filesystem
            // Si le document est rejeté, on déplace le fichier physique hors de la zone de traitement ("Processed" -> "Failed")
            if (!string.IsNullOrEmpty(document.SourceFile))
            {
                await _fileStorage.MoveFileAsync(document.SourceFile, StorageFolder.Processed, StorageFolder.Failed);
            }
        }
        catch (DbUpdateConcurrencyException) { throw new Exception("Conflit lors du rejet. Le document a peut-être déjà été modifié."); }
    }

    private async Task MoveToNextStep(Document document, string roleName, string? comment)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        var statuses = await _unitOfWork.Statuses.GetAllAsync();
        var pendingStatus = statuses.FirstOrDefault(s => s.Code == "BUS_PENDING_VAL") ?? throw new InvalidOperationException("Statut BUS_PENDING_VAL introuvable en base.");

        document.StatusId = pendingStatus.Id;

        await _unitOfWork.Workflows.AddAsync(new Workflow
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            StepName = $"Validation {roleName}",
            CurrentStatus = "BUS_PENDING_VAL",
            AssignedRoleId = role?.Id ?? "",
            ValidatedByUserId = GetUserId(),
            ValidatedAt = DateTime.UtcNow,
            Comment = comment
        });

        try { await _unitOfWork.CompleteAsync(); }
        catch (DbUpdateConcurrencyException) { throw new Exception("Erreur de concurrence lors du passage d'étape."); }
    }

    private async Task FinalizeDocument(Document document, string? comment)
    {
        var statuses = await _unitOfWork.Statuses.GetAllAsync();
        var approvedStatus = statuses.FirstOrDefault(s => s.Code == "APPROVED") ?? throw new InvalidOperationException("Statut APPROVED introuvable en base.");

        document.StatusId = approvedStatus.Id;

        await _unitOfWork.Workflows.AddAsync(new Workflow
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            StepName = "Approbation Finale",
            CurrentStatus = "APPROVED",
            ValidatedByUserId = GetUserId(),
            ValidatedAt = DateTime.UtcNow,
            Comment = comment
        });

        try
        {
            await _unitOfWork.CompleteAsync();

            // 🌟 CORRECTION CRITIQUE : Utilisation de SourceFile et de la nouvelle API StorageFolder
            if (!string.IsNullOrEmpty(document.SourceFile))
            {
                await _fileStorage.MoveFileAsync(document.SourceFile, StorageFolder.Processed, StorageFolder.Archived);
            }
        }
        catch (DbUpdateConcurrencyException) { throw new Exception("Conflit lors de l'approbation finale."); }
    }

    private string[] GetPathForCategory(string? n) => _workflowPaths.GetValueOrDefault(n?.ToUpper() ?? "", new[] { "ROLE_BO", "ROLE_FINANCE" });

    private string? GetUserId() => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public async Task<IEnumerable<WorkflowStepResponse>> GetWorkflowHistoryAsync(Guid id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id);
        if (document == null) return Enumerable.Empty<WorkflowStepResponse>();

        // (Note: J'ai retiré le DTO de ce fichier pour le moment, en supposant que tu le mettes dans ton Dtos.cs plus tard)
        return document.Workflows.Select(w => new Application.DTOs.WorkflowStepResponse(
            w.StepName,
            w.CurrentStatus,
            w.ValidatedByUser?.UserName,
            w.AssignedRole?.Name,
            w.ValidatedAt,
            w.Comment,
            true));
    }

    public async Task<IEnumerable<Document>> GetMyPendingTasksAsync()
    {
        var roles = _httpContextAccessor.HttpContext?.User?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
        var allDocs = await _unitOfWork.Documents.GetAllAsync();
        return allDocs.Where(d => roles.Contains(d.Workflows.OrderByDescending(w => w.ValidatedAt).FirstOrDefault()?.AssignedRole?.Name ?? ""));
    }
}