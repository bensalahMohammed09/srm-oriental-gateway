using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces
{
    public interface IWorkflowService
    {
        // Contrôle du flux BPMN
        Task StartProcessAsync(Guid documentId, string? initialComment);
        Task ApproveStepAsync(Guid documentId, string? comment);
        Task RejectStepAsync(Guid documentId, string reason);

        // Consultations
        Task<IEnumerable<WorkflowStepResponse>> GetWorkflowHistoryAsync(Guid documentId);
        Task<IEnumerable<Document>> GetMyPendingTasksAsync();
    }
}