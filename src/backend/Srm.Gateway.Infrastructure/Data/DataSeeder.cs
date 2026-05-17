using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.Infrastructure.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();

            string[] roles = { "ROLE_ADMIN", "ROLE_BO", "ROLE_FINANCE", "ROLE_TECH" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }

            var testUsers = new Dictionary<string, string>
            {
                { "admin@srm.ma", "ROLE_ADMIN" },
                { "bo@srm.ma", "ROLE_BO" },
                { "finance@srm.ma", "ROLE_FINANCE" },
                { "tech@srm.ma", "ROLE_TECH" }
            };

            var defaultPassword = "Srm_Test_2026!";

            foreach (var userKvp in testUsers)
            {
                await SeedSingleUserAsync(userManager, userKvp.Key, userKvp.Value, defaultPassword);
            }
        }

        private static async Task SeedSingleUserAsync(
            UserManager<IdentityUser<Guid>> userManager,
            string email,
            string role,
            string defaultPassword)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user != null)
            {
                await EnsureUserHasRoleAsync(userManager, user, role);
                return;
            }

            var newUser = new IdentityUser<Guid>
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newUser, defaultPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"[SEEDER FATAL] Failed to create user {email}. Reasons: {errors}");
            }

            var roleResult = await userManager.AddToRoleAsync(newUser, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"[SEEDER FATAL] User created but failed to assign role {role} to {email}: {errors}");
            }
        }

        private static async Task EnsureUserHasRoleAsync(
            UserManager<IdentityUser<Guid>> userManager,
            IdentityUser<Guid> user,
            string role)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}