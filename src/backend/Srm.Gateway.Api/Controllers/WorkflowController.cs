using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Srm.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WorkflowController(IWorkflowService workflowService) : ControllerBase
{
    private readonly IWorkflowService _workflowService = workflowService;

    [HttpGet("my-tasks")]
    [ProducesResponseType(typeof(IEnumerable<DocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTasks()
    {
        // 🌟 FIX : Le service renvoie directement des DocumentResponse enrichis
        var response = await _workflowService.GetMyPendingTasksAsync();
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
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalRequest request)
    {
        await _workflowService.ApproveStepAsync(id, request.Comment);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "Un motif de rejet est obligatoire." });

        await _workflowService.RejectStepAsync(id, request.Reason);
        return NoContent();
    }
}