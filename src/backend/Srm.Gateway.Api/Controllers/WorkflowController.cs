using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;

namespace Srm.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 🛡️ Personne ne touche au workflow sans être connecté
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        var tasks = await _workflowService.GetMyPendingTasksAsync();
        return Ok(tasks);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] WorkflowActionRequest request)
    {
        await _workflowService.ApproveDocumentAsync(id, request.Comment);
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] WorkflowActionRequest request)
    {
        if (string.IsNullOrEmpty(request.Comment))
            return BadRequest("Un motif de rejet est obligatoire.");

        await _workflowService.RejectDocumentAsync(id, request.Comment);
        return NoContent();
    }
}
