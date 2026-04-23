using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Srm.Gateway.Application.Interfaces;

namespace Srm.Gateway.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 🚀 RETOUR À LA SIMPLICITÉ PURE !
public class ProfileController(IProfileService profileService) : ControllerBase
{
    private readonly IProfileService _profileService = profileService;

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentProfile()
    {
        var profile = await _profileService.GetProfileAsync(User);
        return Ok(profile);
    }

    [HttpGet("stats")]
    [Authorize(Roles = "ROLE_ADMIN,ROLE_FINANCE")] // Pur et élégant
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = await _profileService.GetDashboardStatsAsync();
        return Ok(stats);
    }
}