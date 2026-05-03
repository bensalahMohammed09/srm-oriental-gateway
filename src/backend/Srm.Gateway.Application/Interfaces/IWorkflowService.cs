using Srm.Gateway.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces;

public interface IWorkflowService
{
    Task StartProcessAsync(Guid documentId, string? initialComment);
    Task ApproveStepAsync(Guid documentId, string? comment);
    Task RejectStepAsync(Guid documentId, string reason);
    Task<IEnumerable<WorkflowStepResponse>> GetWorkflowHistoryAsync(Guid id);

    // 🌟 FIX : On renvoie maintenant des DTOs enrichis
    Task<IEnumerable<DocumentResponse>> GetMyPendingTasksAsync();
}