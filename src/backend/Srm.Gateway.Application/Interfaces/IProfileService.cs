using Srm.Gateway.Application.DTOs;
using System.Security.Claims;

namespace Srm.Gateway.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(ClaimsPrincipal principal);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}