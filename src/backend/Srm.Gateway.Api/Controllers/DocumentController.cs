using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;

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
}