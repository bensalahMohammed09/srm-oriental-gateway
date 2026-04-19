using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Application.Services;

[ApiController]
[Route("api/v1/[controller]")]
public class DocumentController(IDocumentService service) : ControllerBase
{
    private readonly IDocumentService _service = service;

    [HttpPost("ingest")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Ingest([FromBody] OcrIngestionRequest request)
    {
        var documentId = await _service.IngestDocumentAsync(request);

        return CreatedAtAction(nameof(GetById),new {id =  documentId}, new {id = documentId});
    }

    [HttpPost("{id:guid}/confirm-indexation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmIndexation(Guid id, [FromBody] DocumentValidationRequest request)
    {
        var document = await _service.GetByIdAsync(id);
        if (document == null) return NotFound();

        await _service.ConfirmIndexationAsync(id, request);
        return NoContent();
    }

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
        if(document == null) return NotFound();

        return Ok(document);
    }

    ///<summary>
    ///Receives file from React Dashboard and saves it to the shared volume.
    ///This triggers the Python OCR Worker. No Db entry is created yet
    ///</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided!");

        try
        {
            await _service.SaveFileToPendingAsync(file);

            return Accepted(new
            {
                message = "Upload successful. Processing started.",
                fileName = file.FileName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        try
        {
            var (stream, contentType, fileName) = await _service.GetDocumentFileAsync(id);

            return File(stream, contentType);
        }
        catch(FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    [HttpPost("failed/recover")]
    public async Task<IActionResult> RecoverFailedDocument([FromBody] ManualRecoveryRequest request)
    {
        try
        {
            var documentId = await _service.RecoverFailedDocumentAsync(request);

            return Ok(new
            {
                message = "Document récupéré et réintégré avec succès.",
                documentId = documentId
            });
        }
        catch (FileNotFoundException ex)
        {
            // Si le fichier n'est pas dans le dossier /failed
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Erreur de base de données ou autre
            return StatusCode(500, new { error = $"Erreur lors de la récupération : {ex.Message}" });
        }
    }

    [HttpGet("failed")]
    public async Task<IActionResult> GetFailedFiles()
    {
        try
        {
            var files = await _service.GetFailedFilesAsync();
            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Erreur lors de la lecture du dossier d'échec : {ex.Message}" });
        }
    }
    [HttpGet("failed/{fileName}/file")]
    public async Task<IActionResult> GetFailedFile(string fileName)
    {
        try
        {
            // Attention au décodage de l'URL si le nom du fichier contient des espaces !
            // ASP.NET s'en charge généralement tout seul.
            var (stream, contentType) = await _service.GetFailedDocumentFileAsync(fileName);

            // Comme pour processed, on ne met pas 'fileName' ici pour forcer l'affichage inline
            return File(stream, contentType);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Erreur lors de la récupération du fichier en échec : {ex.Message}" });
        }
    }
}