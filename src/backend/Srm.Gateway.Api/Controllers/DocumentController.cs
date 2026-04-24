using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _service;
    private readonly IDocumentMetadataService _metadataService;

    public DocumentController(IDocumentService service, IDocumentMetadataService metadataService)
    {
        _service = service;
        _metadataService = metadataService;
    }

    [HttpPost("ingest")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Ingest([FromBody] OcrIngestionRequest request)
    {
        var documentId = await _service.IngestDocumentAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = documentId }, new { id = documentId });
    }

    [HttpPost("{id:guid}/confirm-indexation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmIndexation(Guid id, [FromBody] DocumentValidationRequest request)
    {
        var document = await _service.GetByIdAsync(id);
        if (document == null) return NotFound();

        // 🌟 La validation métier lance le workflow. (Le fichier est déjà dans Processed grâce à l'OCR)
        await _service.ConfirmIndexationAsync(id, request);
        return NoContent();
    }

    // ==========================================
    // 🌟 ENDPOINTS SPLIT-SCREEN & METADATA (JSONB)
    // ==========================================

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<DocumentDetailsResponse>> GetDetails(Guid id)
    {
        // 🌟 L'endpoint ultime pour le Frontend : Renvoie TOUT le document d'un coup
        var document = await _service.GetDocumentDetailsAsync(id);
        if (document == null) return NotFound(new { error = "Document introuvable." });

        return Ok(document);
    }

    [HttpPut("{id:guid}/metadata")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMetadata(Guid id, [FromBody] UpdateMetadataRequest request)
    {
        try
        {
            // 🌟 FIX CS1503 : Mapping du DTO Frontend vers l'Entité Backend
            var domainMetadata = request.NewMetadata.ToDictionary(
                kvp => kvp.Key,
                kvp => new Srm.Gateway.Domain.Entities.DocumentFieldValue
                {
                    Value = kvp.Value.Value,
                    Confidence = kvp.Value.Confidence
                }
            );

            // Approche "Clear & Replace"
            await _metadataService.UpdateMetadataAsync(id, domainMetadata);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ==========================================
    // 🌟 ENDPOINTS CLASSIQUES (LISTES & FICHIERS)
    // ==========================================

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<DocumentResponse>>> GetPending()
    {
        var results = await _service.GetPendingIndexationAsync();
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentResponse>> GetById(Guid id)
    {
        var document = await _service.GetByIdAsync(id);
        if (document == null) return NotFound();

        return Ok(document);
    }

    ///<summary>
    /// Reçoit le fichier depuis React (Bouton Upload OCR) et le sauvegarde dans le volume partagé (Pending).
    ///</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided!");

        await _service.SaveFileToPendingAsync(file);

        return Accepted(new
        {
            message = "Upload successful. Processing started.",
            fileName = file.FileName
        });
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        try
        {
            var fileDto = await _service.GetFileForViewAsync(id);
            return File(fileDto.Stream, fileDto.ContentType, fileDto.FileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("failed/recover")]
    public async Task<IActionResult> RecoverFailedDocument([FromBody] ManualRecoveryRequest request)
    {
        try
        {
            var documentId = await _service.RecoverFailedDocumentAsync(request);
            return Ok(new { message = "Document récupéré.", documentId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("failed")]
    [ProducesResponseType(typeof(IEnumerable<FailedFileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedFiles()
    {
        var files = await _service.GetFailedFilesAsync();
        return Ok(files);
    }

    [HttpGet("failed/{fileName}/file")]
    public async Task<IActionResult> GetFailedFile(string fileName)
    {
        try
        {
            var fileDto = await _service.GetFailedDocumentFileAsync(fileName);
            return File(fileDto.Stream, fileDto.ContentType, fileDto.FileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // 🌟 CORRECTION : Formulaire Data-Only pour la Saisie Manuelle (Sans IFormFile)
    [HttpPost("manual-upload")]
    public async Task<IActionResult> ManualUpload([FromBody] ManualUploadRequest request)
    {
        try
        {
            var documentId = await _service.CreateManualDocumentAsync(request);
            return Ok(new { id = documentId, message = "Saisie manuelle créée avec succès." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/view")]
    public async Task<IActionResult> ViewFile(Guid id)
    {
        try
        {
            var fileDto = await _service.GetFileForViewAsync(id);
            // enableRangeProcessing permet de streamer de gros PDFs efficacement
            return File(fileDto.Stream, fileDto.ContentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] string? status)
    {
        var results = await _service.SearchDocumentsAsync(query, status);
        return Ok(results);
    }
}