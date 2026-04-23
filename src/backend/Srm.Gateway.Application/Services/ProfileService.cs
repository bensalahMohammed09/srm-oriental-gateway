using Microsoft.AspNetCore.Identity;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Srm.Gateway.Application.Services;

public class ProfileService(
    UserManager<IdentityUser> userManager,
    IUnitOfWork unitOfWork) : IProfileService
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<UserProfileDto> GetProfileAsync(ClaimsPrincipal principal)
    {
        // 🛡️ CORRECTION SRE : Double vérification (Format Microsoft + Format Standard JWT)
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? throw new UnauthorizedAccessException("Utilisateur non identifié dans le jeton.");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("Utilisateur introuvable dans la base de données.");

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileDto(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles,
            DateTime.UtcNow
        );
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var allDocs = await _unitOfWork.Documents.GetAllAsync();

        return new DashboardStatsDto(
            TotalDocuments: allDocs.Count(),
            PendingValidation: allDocs.Count(d => d.Status.Code == "BUS_PENDING_VAL"),
            ApprovedAmount: allDocs.Where(d => d.Status.Code == "APPROVED").Sum(d => d.TotalAmount ?? 0m),
            RejectedCount: allDocs.Count(d => d.Status.Code == "REJECTED"),
            Distribution: allDocs.GroupBy(d => d.Category.Name)
                                .Select(g => new CategoryDistributionDto(g.Key, g.Count())),
            RecentActivity: allDocs.OrderByDescending(d => d.CreatedAt)
                                  .Take(5)
                                  .Select(d => new RecentActivityDto(d.Reference, d.SupplierName, d.Status.Code, d.CreatedAt))
        );
    }
}