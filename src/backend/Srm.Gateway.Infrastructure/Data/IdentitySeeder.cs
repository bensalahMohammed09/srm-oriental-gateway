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
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "ROLE_ADMIN", "ROLE_BO", "ROLE_FINANCE", "ROLE_TECH" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
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
                var email = userKvp.Key;
                var role = userKvp.Value;

                var user = await userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    var newUser = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(newUser, defaultPassword);

                    if (result.Succeeded)
                    {
                        var roleResult = await userManager.AddToRoleAsync(newUser, role);
                        if (!roleResult.Succeeded)
                        {
                            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                            throw new Exception($"[SEEDER FATAL] User created but failed to assign role {role} to {email}: {errors}");
                        }
                    }
                    else
                    {
                        // 💥 WE NOW THROW AN EXCEPTION SO YOU CAN SEE THE EXACT MICROSOFT IDENTITY ERROR
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"[SEEDER FATAL] Failed to create user {email}. Reasons: {errors}");
                    }
                }
                else
                {
                    var currentRoles = await userManager.GetRolesAsync(user);
                    if (!currentRoles.Contains(role))
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                }
            }
        }
    }
}