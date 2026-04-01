using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AspNetCoreMvcTemplate.Web.Services
{
    public class AdminBootstrapSeeder
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IOptions<AdminBootstrapOptions> options;
        private readonly ILogger<AdminBootstrapSeeder> logger;

        public AdminBootstrapSeeder(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<AdminBootstrapOptions> options,
            ILogger<AdminBootstrapSeeder> logger)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.options = options;
            this.logger = logger;
        }

        public async Task SeedAsync()
        {
            var adminBootstrapOptions = options.Value;

            if (!adminBootstrapOptions.Enabled)
            {
                logger.LogDebug("Admin bootstrap seeding is disabled.");
                return;
            }

            await EnsureRoleExistsAsync(adminBootstrapOptions.RoleName);

            var adminUser = await userManager.FindByEmailAsync(adminBootstrapOptions.Email);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminBootstrapOptions.Email,
                    Email = adminBootstrapOptions.Email,
                    Name = adminBootstrapOptions.DisplayName,
                    EmailConfirmed = true
                };

                var createUserResult = await userManager.CreateAsync(adminUser, adminBootstrapOptions.Password);
                if (!createUserResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create bootstrap admin user '{adminBootstrapOptions.Email}': {string.Join("; ", createUserResult.Errors.Select(x => x.Description))}");
                }

                logger.LogInformation("Created bootstrap admin user {Email}.", adminBootstrapOptions.Email);
            }
            else
            {
                var shouldUpdateUser = false;

                if (!string.Equals(adminUser.Name, adminBootstrapOptions.DisplayName, StringComparison.Ordinal))
                {
                    adminUser.Name = adminBootstrapOptions.DisplayName;
                    shouldUpdateUser = true;
                }

                if (!adminUser.EmailConfirmed)
                {
                    adminUser.EmailConfirmed = true;
                    shouldUpdateUser = true;
                }

                if (shouldUpdateUser)
                {
                    var updateUserResult = await userManager.UpdateAsync(adminUser);
                    if (!updateUserResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Failed to update bootstrap admin user '{adminBootstrapOptions.Email}': {string.Join("; ", updateUserResult.Errors.Select(x => x.Description))}");
                    }

                    logger.LogInformation("Updated bootstrap admin user {Email}.", adminBootstrapOptions.Email);
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, adminBootstrapOptions.RoleName))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminBootstrapOptions.RoleName);
                if (!addToRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to assign bootstrap admin user '{adminBootstrapOptions.Email}' to role '{adminBootstrapOptions.RoleName}': {string.Join("; ", addToRoleResult.Errors.Select(x => x.Description))}");
                }

                logger.LogInformation(
                    "Assigned bootstrap admin user {Email} to role {RoleName}.",
                    adminBootstrapOptions.Email,
                    adminBootstrapOptions.RoleName);
            }
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create bootstrap admin role '{roleName}': {string.Join("; ", createRoleResult.Errors.Select(x => x.Description))}");
            }

            logger.LogInformation("Created bootstrap admin role {RoleName}.", roleName);
        }
    }
}
