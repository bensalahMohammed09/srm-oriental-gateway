using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Srm.Gateway.Application.Services;

public class ProfileService(
    UserManager<IdentityUser<Guid>> userManager,
    IUnitOfWork unitOfWork) : IProfileService
{
    private readonly UserManager<IdentityUser<Guid>> _userManager = userManager;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<UserProfileDto> GetProfileAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? throw new UnauthorizedAccessException("Utilisateur non identifié dans le jeton.");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("Utilisateur introuvable dans la base de données.");

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileDto(
            user.Id.ToString(),
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles,
            DateTime.UtcNow
        );
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        // 🌟 REFACTORED: Offloaded all aggregations to SQL instead of loading the entire DB into RAM.
        // This makes the dashboard load in milliseconds regardless of database size.

        var totalDocs = await _unitOfWork.Documents.FindByCondition(d => true).CountAsync();

        var pendingValidation = await _unitOfWork.Documents
            .FindByCondition(d => d.Status != null && d.Status.Code == "BUS_PENDING_VAL")
            .CountAsync();

        var approvedAmount = await _unitOfWork.Documents
            .FindByCondition(d => d.Status != null && d.Status.Code == "APPROVED")
            .SumAsync(d => d.TotalAmount ?? 0m);

        var rejectedCount = await _unitOfWork.Documents
            .FindByCondition(d => d.Status != null && d.Status.Code == "REJECTED")
            .CountAsync();

        var distribution = await _unitOfWork.Documents
            .FindByCondition(d => d.Category != null)
            .GroupBy(d => d.Category!.Name)
            .Select(g => new CategoryDistributionDto(g.Key, g.Count()))
            .ToListAsync();

        var recentActivity = await _unitOfWork.Documents
            .FindByCondition(d => true)
            .Include(d => d.Status)
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => new RecentActivityDto(
                d.Id, // 🌟 AJOUT : L'ID est essentiel pour les liens vers les détails du document !
                d.Reference,
                d.SupplierName ?? "Inconnu",
                d.Status != null ? d.Status.Code : "UNKNOWN",
                d.CreatedAt))
            .ToListAsync();

        return new DashboardStatsDto(
            totalDocs,
            pendingValidation,
            approvedAmount,
            rejectedCount,
            distribution,
            recentActivity
        );
    }
}