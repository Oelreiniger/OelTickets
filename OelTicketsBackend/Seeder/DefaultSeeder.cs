using Microsoft.AspNetCore.Identity;
using OelTicketsBackend.Auth;

namespace OelTicketsBackend.Seeder
{
    internal class DefaultSeeder
    {
        public static async Task SeedDB(IServiceProvider sp)
        {
            await SeedRolesAsync(sp);
            await SeedAdmin(sp);
        }

        public static async Task SeedRolesAsync(IServiceProvider sp)
        {
            List<string> defaultRoles = new List<string>
            {
                "Admin",
                "Dev",
                "Customer",
            };

            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (string role in defaultRoles)
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole(role));
        }

        public static async Task SeedAdmin(IServiceProvider sp)
        {
            var defaultAdmin = new ApplicationUser
            {
                UserName = "Admin",
                Email = "admin@mail.com",
                FirstName = "Admin",
                LastName = "Admin"
            };

            using var scope = sp.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var result = await userManager.CreateAsync(defaultAdmin, "#Admin4dminpw");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultAdmin, "Admin");
            }
        }
    }
}
