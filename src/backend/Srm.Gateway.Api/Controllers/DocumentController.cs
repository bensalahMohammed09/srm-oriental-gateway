using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(IDocumentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var docs = await service.GetAllDocumentsAsync();
        return Ok(docs);
    }
    [HttpGet("{reference}")]
    public async Task<IActionResult> GetByReference(string reference)
    {
        var doc = await service.GetByReferenceAsync(reference);
        return doc != null ? Ok(doc) : NotFound();
    }
    [HttpGet("debug-exception")]
    public IActionResult TriggerError()
    {
        // On simule une erreur critique de logique ou de base de données
        throw new Exception("Simulated error for SRM Oriental Gateway observability test.");
    }
}