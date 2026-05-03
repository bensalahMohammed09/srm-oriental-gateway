using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.Interfaces;

namespace Srm.Gateway.Api.Controllers
{
    [ApiController]
    [Route("api/n8n")]
    public class N8nController : ControllerBase
    {
        private readonly IN8nService _n8nService;

        public N8nController(IN8nService n8nService)
        {
            _n8nService = n8nService;
        }

        [HttpGet("pending-escalations")]
        public async Task<IActionResult> GetPending()
        {
            var data = await _n8nService.GetPendingEscalationsAsync();
            return Ok(data);
        }

        [HttpPut("increment-level/{id}")]
        public async Task<IActionResult> Increment(Guid id)
        {
            var result = await _n8nService.IncrementLevelAsync(id);
            return result ? Ok() : NotFound();
        }
    }
}
