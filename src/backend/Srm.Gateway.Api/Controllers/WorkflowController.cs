using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 🛡️ Accès restreint aux utilisateurs authentifiés
public class WorkflowController(IWorkflowService workflowService) : ControllerBase
{
    private readonly IWorkflowService _workflowService = workflowService;

    [HttpGet("my-tasks")]
    [ProducesResponseType(typeof(IEnumerable<DocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTasks()
    {
        var tasks = await _workflowService.GetMyPendingTasksAsync();

        // ✅ Mapping explicite vers le DTO de réponse pour le Frontend React
        var response = tasks.Select(d => new DocumentResponse(
            d.Id,
            d.Reference,
            d.Status?.Name ?? "Statut inconnu",
            d.Category?.Name ?? "Non catégorisé",
            d.CreatedAt
        ));

        return Ok(response);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<WorkflowStepResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var history = await _workflowService.GetWorkflowHistoryAsync(id);
        return Ok(history);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalRequest request)
    {
        try
        {
            await _workflowService.ApproveStepAsync(id, request.Comment);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("Conflit"))
        {
            // 🛡️ Gestion de la concurrence optimiste (RowVersion)
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "Un motif de rejet est obligatoire pour renvoyer le dossier au Bureau d'Ordre." });

        try
        {
            await _workflowService.RejectStepAsync(id, request.Reason);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("Conflit"))
        {
            return Conflict(new { error = ex.Message });
        }
    }
}