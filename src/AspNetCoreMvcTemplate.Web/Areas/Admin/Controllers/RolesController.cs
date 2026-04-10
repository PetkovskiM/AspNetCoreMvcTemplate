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
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AdminBootstrapOptions bootstrapOptions;

        public RolesController(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IOptions<AdminBootstrapOptions> bootstrapOptions)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.bootstrapOptions = bootstrapOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            var roles = roleManager.Roles.ToList();
            var items = new List<RoleListItemViewModel>();

            foreach (var role in roles)
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
                items.Add(new RoleListItemViewModel
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    UserCount = usersInRole.Count,
                    IsProtected = IsProtectedRole(role.Name)
                });
            }

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateRoleViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await roleManager.RoleExistsAsync(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), $"A role named '{model.Name}' already exists.");
                return View(model);
            }

            var result = await roleManager.CreateAsync(new IdentityRole(model.Name));
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
            {
                return NotFound();
            }

            var model = new EditRoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                IsProtected = IsProtectedRole(role.Name)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var role = await roleManager.FindByIdAsync(model.Id);
            if (role is null)
            {
                return NotFound();
            }

            if (IsProtectedRole(role.Name))
            {
                model.IsProtected = true;
                ModelState.AddModelError(string.Empty, $"The '{role.Name}' role is protected and cannot be renamed.");
                return View(model);
            }

            role.Name = model.Name;
            var result = await roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
            {
                return NotFound();
            }

            var usersInRole = await userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

            var model = new DeleteRoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = usersInRole.Count,
                IsProtected = IsProtectedRole(role.Name)
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role is null)
            {
                return NotFound();
            }

            var usersInRole = await userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);

            var model = new DeleteRoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = usersInRole.Count,
                IsProtected = IsProtectedRole(role.Name)
            };

            if (model.IsProtected)
            {
                ModelState.AddModelError(string.Empty, $"The '{role.Name}' role is protected and cannot be deleted.");
                return View(model);
            }

            if (model.UserCount > 0)
            {
                ModelState.AddModelError(string.Empty, $"Cannot delete the '{role.Name}' role because it has {model.UserCount} user(s) assigned.");
                return View(model);
            }

            var result = await roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool IsProtectedRole(string? roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return false;
            }

            return string.Equals(roleName, bootstrapOptions.RoleName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
