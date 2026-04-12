using AspNetCoreMvcTemplate.Web.Areas.Admin.ViewModels;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AspNetCoreMvcTemplate.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly AdminBootstrapOptions bootstrapOptions;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<AdminBootstrapOptions> bootstrapOptions)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.bootstrapOptions = bootstrapOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            var users = userManager.Users.ToList();
            var items = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                items.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Name = user.Name,
                    Roles = string.Join(", ", roles.OrderBy(r => r)),
                    EmailConfirmed = user.EmailConfirmed,
                    IsProtected = IsProtectedUser(user)
                });
            }

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            var model = await BuildEditViewModelAsync(user);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RoleAssignments = BuildRoleAssignments(model.RoleAssignments);
                return View(model);
            }

            var user = await userManager.FindByIdAsync(model.Id);
            if (user is null)
            {
                return NotFound();
            }

            var isProtected = IsProtectedUser(user);

            if (isProtected && !model.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Cannot disable email confirmation for the bootstrap admin user.");
                model = await BuildEditViewModelAsync(user);
                return View(model);
            }

            if (isProtected && model.LockoutEnabled)
            {
                ModelState.AddModelError(string.Empty, "Cannot enable lockout for the bootstrap admin user.");
                model = await BuildEditViewModelAsync(user);
                return View(model);
            }

            user.Name = model.Name.Trim();
            user.EmailConfirmed = model.EmailConfirmed;
            user.LockoutEnabled = model.LockoutEnabled;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model = await BuildEditViewModelAsync(user);
                return View(model);
            }

            await SyncRoleAssignmentsAsync(user, model.RoleAssignments, isProtected);

            return RedirectToAction(nameof(Index));
        }

        private async Task SyncRoleAssignmentsAsync(
            ApplicationUser user,
            List<RoleAssignmentViewModel> assignments,
            bool isProtected)
        {
            var currentRoles = await userManager.GetRolesAsync(user);

            foreach (var assignment in assignments)
            {
                var isCurrentlyInRole = currentRoles.Contains(assignment.RoleName, StringComparer.OrdinalIgnoreCase);

                if (assignment.IsAssigned && !isCurrentlyInRole)
                {
                    await userManager.AddToRoleAsync(user, assignment.RoleName);
                }
                else if (!assignment.IsAssigned && isCurrentlyInRole)
                {
                    if (isProtected && string.Equals(assignment.RoleName, bootstrapOptions.RoleName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    await userManager.RemoveFromRoleAsync(user, assignment.RoleName);
                }
            }
        }

        private async Task<EditUserViewModel> BuildEditViewModelAsync(ApplicationUser user)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            var allRoles = roleManager.Roles.OrderBy(r => r.Name).ToList();

            return new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Name = user.Name,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                IsProtected = IsProtectedUser(user),
                RoleAssignments = allRoles.Select(role => new RoleAssignmentViewModel
                {
                    RoleName = role.Name ?? string.Empty,
                    IsAssigned = userRoles.Contains(role.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                }).ToList()
            };
        }

        private List<RoleAssignmentViewModel> BuildRoleAssignments(
            List<RoleAssignmentViewModel> submitted)
        {
            var allRoles = roleManager.Roles.OrderBy(r => r.Name).ToList();

            return allRoles.Select(role => new RoleAssignmentViewModel
            {
                RoleName = role.Name ?? string.Empty,
                IsAssigned = submitted.Any(s =>
                    string.Equals(s.RoleName, role.Name, StringComparison.OrdinalIgnoreCase) && s.IsAssigned)
            }).ToList();
        }

        private bool IsProtectedUser(ApplicationUser user)
        {
            return string.Equals(user.Email, bootstrapOptions.Email, StringComparison.OrdinalIgnoreCase);
        }
    }
}
