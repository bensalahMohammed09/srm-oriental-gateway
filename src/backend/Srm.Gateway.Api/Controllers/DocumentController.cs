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
}