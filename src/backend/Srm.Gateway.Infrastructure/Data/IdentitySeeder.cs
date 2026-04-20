using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Infrastructure.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Rôles BPMN SRM
            string[] roles = { "ROLE_ADMIN", "ROLE_BO", "ROLE_FINANCE", "ROLE_TECH" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Administrateur Racine
            var adminEmail = "admin@srm.ma";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Respecte la politique de 12 caractères
                var result = await userManager.CreateAsync(newAdmin, "Srm_Admin_2026!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "ROLE_ADMIN");
                }
            }
        }
    }
}
