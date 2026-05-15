using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace Srm.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Protégé par défaut
public class DocumentController(
    IDocumentQueryService queryService,
    IDocumentCommandService commandService) : ControllerBase
{
    private readonly IDocumentQueryService _queryService = queryService;
    private readonly IDocumentCommandService _commandService = commandService;

    // --- QUERIES (GET) ---

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] string? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _queryService.SearchDocumentsAsync(query, status, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { detail = "Aucun fichier fourni." });
        }

        try
        {
            await _commandService.SaveFileToPendingAsync(file);
            return Ok(new { message = "Fichier uploadé avec succès dans le dossier pending.", fileName = file.FileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = $"Erreur lors de l'upload : {ex.Message}" });
        }
    }

    [HttpGet("pending-indexation")]
    [Authorize(Roles = "ROLE_BO,ROLE_ADMIN")] // Restreint au Back-Office
    public async Task<IActionResult> GetPendingIndexation([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _queryService.GetPendingIndexationAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}/details")]
    public async Task<IActionResult> GetDocumentDetails(Guid id)
    {
        var result = await _queryService.GetDocumentDetailsAsync(id);
        return Ok(result);
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        var fileData = await _queryService.GetFileForViewAsync(id);
        return File(fileData.Stream, fileData.ContentType, fileData.FileName);
    }

    [HttpGet("failed")]
    [Authorize(Roles = "ROLE_BO,ROLE_ADMIN")]
    public async Task<IActionResult> GetFailedFiles()
    {
        var result = await _queryService.GetFailedFilesAsync();
        return Ok(result);
    }

    [HttpGet("failed/{fileName}/file")]
    [Authorize(Roles = "ROLE_BO,ROLE_ADMIN")]
    public async Task<IActionResult> DownloadFailedFile(string fileName)
    {
        // Décodage du nom de fichier (ex: espaces, accents)
        var decodedFileName = Uri.UnescapeDataString(fileName);
        var fileData = await _queryService.GetFailedDocumentFileAsync(decodedFileName);
        return File(fileData.Stream, fileData.ContentType, fileData.FileName);
    }

    // --- COMMANDS (POST/PUT) ---

    [HttpPost("ingest")]
    [AllowAnonymous]
    public async Task<IActionResult> IngestFromOcr([FromBody] OcrIngestionRequest request)
    {
        var id = await _commandService.IngestDocumentAsync(request);
        return CreatedAtAction(nameof(GetDocumentDetails), new { id }, new { id, message = "Ingestion réussie" });
    }

    [HttpPut("{id:guid}/confirm-indexation")]
    [Authorize(Roles = "ROLE_BO,ROLE_ADMIN")]
    public async Task<IActionResult> ConfirmIndexation(Guid id, [FromBody] DocumentValidationRequest request)
    {
        await _commandService.ConfirmIndexationAsync(id, request);
        return NoContent();
    }

    [HttpPost("manual-upload")]
    [Authorize(Roles = "ROLE_BO,ROLE_ADMIN")]
    public async Task<IActionResult> CreateManualDocument([FromBody] ManualUploadRequest request)
    {
        var id = await _commandService.CreateManualDocumentAsync(request);
        return CreatedAtAction(nameof(GetDocumentDetails), new { id }, new { id, message = "Création manuelle réussie" });
    }

    // 🌟 LA ROUTE QUI MANQUAIT EST LÀ ! 🌟
    [HttpPost("failed/recover")]
    [Authorize(Roles = "ROLE_BO,ROLE_ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecoverFailedDocument([FromBody] ManualRecoveryRequest request)
    {
        try
        {
            var documentId = await _commandService.RecoverFailedDocumentAsync(request);
            return Ok(new { id = documentId, message = "Récupération manuelle réussie" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }
}