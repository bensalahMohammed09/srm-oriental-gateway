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
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IFileStorageService _fileStorage;
    private readonly IWorkflowEngineHelper _engineHelper;

    public WorkflowService(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        RoleManager<IdentityRole<Guid>> roleManager,
        IFileStorageService fileStorage,
        IWorkflowEngineHelper engineHelper)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _roleManager = roleManager;
        _fileStorage = fileStorage;
        _engineHelper = engineHelper;
    }

    public async Task StartProcessAsync(Guid documentId, string? initialComment)
    {
        var document = await _unitOfWork.Documents.FindByCondition(d => d.Id == documentId, trackChanges: true)
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");

        var pendingStatus = await _unitOfWork.Repository<Status>().FindByCondition(s => s.Code == "BUS_PENDING_VAL").FirstOrDefaultAsync();
        document.StatusId = pendingStatus!.Id;

        var boRole = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == "ROLE_BO");

        await _unitOfWork.Workflows.AddAsync(new Workflow
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            StepName = "Indexation terminée, envoi pour validation parallèle.",
            CurrentStatus = "BUS_PENDING_VAL",
            AssignedRoleId = boRole!.Id,
            ValidatedByUserId = GetUserId(),
            ValidatedAt = DateTime.UtcNow,
            Comment = initialComment ?? string.Empty
        });

        await _unitOfWork.CompleteAsync();
    }

    public Task ApproveStepAsync(Guid documentId, string? comment) => RegisterVoteAsync(documentId, comment, true);

    public Task RejectStepAsync(Guid documentId, string reason) => RegisterVoteAsync(documentId, reason, false);

    private async Task RegisterVoteAsync(Guid documentId, string? comment, bool isApproved)
    {
        var document = await _unitOfWork.Documents.FindByCondition(d => d.Id == documentId, trackChanges: true)
            .Include(d => d.Category)
            .Include(d => d.Workflows).ThenInclude(w => w.AssignedRole)
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException("Document introuvable.");

        var targetRoles = _engineHelper.GetTargetRoles(document.Category?.Name);
        var userRoles = GetUserRoles();

        var myRoleName = userRoles.Find(r => targetRoles.Contains(r));

        if (myRoleName == null)
            throw new InvalidOperationException("Vous n'êtes pas autorisé à valider ce document ou n'êtes pas dans le circuit.");

        var currentCycleStart = _engineHelper.GetCurrentCycleStart(document);

        if (_engineHelper.HasUserVotedInCurrentCycle(document, myRoleName, currentCycleStart))
            throw new InvalidOperationException("Vous avez déjà pris une décision pour ce dossier dans le cycle actuel.");

        var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == myRoleName);
        var stepStatus = isApproved ? "APPROVED" : "REJECTED";
        var stepName = isApproved ? $"Approuvé par {myRoleName.Replace("ROLE_", "")}" : $"Rejeté par {myRoleName.Replace("ROLE_", "")}";

        await _unitOfWork.Workflows.AddAsync(new Workflow
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            StepName = stepName,
            CurrentStatus = stepStatus,
            AssignedRoleId = role!.Id,
            ValidatedByUserId = GetUserId(),
            ValidatedAt = DateTime.UtcNow,
            Comment = comment ?? string.Empty
        });

        var existingVotes = document.Workflows
            .Where(w => (w.CurrentStatus == "APPROVED" || w.CurrentStatus == "REJECTED") && w.ValidatedAt >= currentCycleStart)
            .Select(w => w.AssignedRole?.Name)
            .ToList();

        existingVotes.Add(myRoleName);

        var distinctVoters = existingVotes.Distinct().ToList();
        bool allDepartmentsVoted = targetRoles.All(tr => distinctVoters.Contains(tr));

        if (allDepartmentsVoted)
        {
            var allStatuses = document.Workflows
                .Where(w => (w.CurrentStatus == "APPROVED" || w.CurrentStatus == "REJECTED") && w.ValidatedAt >= currentCycleStart)
                .Select(w => w.CurrentStatus)
                .ToList();

            allStatuses.Add(stepStatus);

            bool anyRejected = allStatuses.Any(s => s == "REJECTED");
            string finalStatusCode = anyRejected ? "REJECTED" : "APPROVED";

            var finalStatus = await _unitOfWork.Repository<Status>().FindByCondition(s => s.Code == finalStatusCode).FirstOrDefaultAsync();
            document.StatusId = finalStatus!.Id;

            await _unitOfWork.Workflows.AddAsync(new Workflow
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                StepName = anyRejected ? "Rejet Définitif" : "Approbation Finale",
                CurrentStatus = finalStatusCode,
                AssignedRoleId = role.Id,
                ValidatedByUserId = GetUserId(),
                ValidatedAt = DateTime.UtcNow,
                Comment = anyRejected ? "Dossier rejeté par un ou plusieurs départements." : "Tous les départements ont approuvé."
            });

            if (finalStatusCode == "APPROVED" && !string.IsNullOrEmpty(document.SourceFile))
                await _fileStorage.MoveFileAsync(document.SourceFile, StorageFolder.Processed, StorageFolder.Archived);
            else if (finalStatusCode == "REJECTED" && !string.IsNullOrEmpty(document.SourceFile))
                await _fileStorage.MoveFileAsync(document.SourceFile, StorageFolder.Processed, StorageFolder.Failed);
        }

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException ex)
        {
            // Fixed raw System.Exception code smell (S112)
            throw new InvalidOperationException($"Erreur SQL: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<IEnumerable<DocumentResponse>> GetMyPendingTasksAsync()
    {
        var roles = GetUserRoles();

        // Fixed S3267: Prefer comparing 'Count' to 0 for lists over '.Any()'
        if (roles.Count == 0) return Enumerable.Empty<DocumentResponse>();

        var pendingStatus = await _unitOfWork.Repository<Status>().FindByCondition(s => s.Code == "BUS_PENDING_VAL").AsNoTracking().FirstOrDefaultAsync();
        var rejectedStatus = await _unitOfWork.Repository<Status>().FindByCondition(s => s.Code == "REJECTED").AsNoTracking().FirstOrDefaultAsync();

        if (pendingStatus == null || rejectedStatus == null) return Enumerable.Empty<DocumentResponse>();

        var activeDocs = await _unitOfWork.Documents
            .FindByCondition(d => d.StatusId == pendingStatus.Id || d.StatusId == rejectedStatus.Id)
            .AsNoTracking()
            .Include(d => d.Status)
            .Include(d => d.Category)
            .Include(d => d.Workflows).ThenInclude(w => w.AssignedRole)
            .ToListAsync();

        var myTasks = new List<DocumentResponse>();

        foreach (var doc in activeDocs)
        {
            // Guard 1: Ignore documents that are not currently in the validation pending state
            if (doc.StatusId != pendingStatus.Id) continue;

            var targetRoles = _engineHelper.GetTargetRoles(doc.Category?.Name);

            // Fixed S6602: Prefer '.Find' over '.FirstOrDefault' for List lookup
            var myTargetRole = roles.Find(r => targetRoles.Contains(r));

            // Guard 2: Ignore if current user holds no validation role in this circuit
            if (myTargetRole == null) continue;

            var currentCycleStart = _engineHelper.GetCurrentCycleStart(doc);

            bool hasVoted = _engineHelper.HasUserVotedInCurrentCycle(doc, myTargetRole, currentCycleStart);

            // Guard 3: Ignore if the user has already submitted their vote inside this cycle
            if (hasVoted) continue;

            var approvalsDict = _engineHelper.BuildApprovalsDictionary(doc, targetRoles, currentCycleStart);

            myTasks.Add(new DocumentResponse(
                doc.Id,
                doc.Reference,
                doc.Status?.Code ?? "UNKNOWN",
                doc.Category?.Name,
                doc.CreatedAt,
                approvalsDict
            ));
        }

        return myTasks.OrderByDescending(d => d.CreatedAt);
    }

    private List<string> GetUserRoles()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }

    private Guid? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdStr = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user?.FindFirst("sub")?.Value
                     ?? user?.FindFirst("uid")?.Value;

        if (Guid.TryParse(userIdStr, out Guid userId)) return userId;
        return null;
    }

    public async Task<IEnumerable<WorkflowStepResponse>> GetWorkflowHistoryAsync(Guid id)
    {
        var document = await _unitOfWork.Documents.FindByCondition(d => d.Id == id)
            .AsNoTracking()
            .Include(d => d.Workflows).ThenInclude(w => w.AssignedRole)
            .Include(d => d.Workflows).ThenInclude(w => w.ValidatedByUser)
            .FirstOrDefaultAsync();

        if (document == null) return Enumerable.Empty<WorkflowStepResponse>();

        return document.Workflows.Select(w => new WorkflowStepResponse(
            w.StepName,
            w.CurrentStatus,
            w.ValidatedByUser?.UserName,
            w.AssignedRole?.Name,
            w.ValidatedAt,
            w.Comment,
            true));
    }
}
