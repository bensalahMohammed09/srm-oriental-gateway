using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Srm.Gateway.Application.Interfaces
{
    public interface IWorkflowService
    {
        // Récupérer les tâches en attente pour l'utilisateur connecté (Basé sur son rôle)
        Task<IEnumerable<Document>> GetMyPendingTasksAsync();

        // Action d'approbation (Avancement dans le BPMN)
        Task ApproveDocumentAsync(Guid documentId, string? comment);

        // Action de rejet (Retour au Bureau d'Ordre)
        Task RejectDocumentAsync(Guid documentId, string reason);
    }
}
