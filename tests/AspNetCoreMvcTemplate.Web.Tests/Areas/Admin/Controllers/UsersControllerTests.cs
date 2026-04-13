using AspNetCoreMvcTemplate.Web.Areas.Admin.Controllers;
using AspNetCoreMvcTemplate.Web.Areas.Admin.ViewModels;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Areas.Admin.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task Edit_Get_WhenUserNotFound_ReturnsNotFound()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager.Object, CreateRoleManagerMock().Object);

        var result = await controller.Edit("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WhenIdIsEmpty_ReturnsNotFound()
    {
        var controller = CreateController(CreateUserManagerMock().Object, CreateRoleManagerMock().Object);

        var result = await controller.Edit("");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsViewWithUserDataAndRoles()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "user@test.local",
            UserName = "user@test.local",
            Name = "Test User",
            EmailConfirmed = true,
            LockoutEnabled = false
        };

        userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var roles = new List<IdentityRole>
        {
            new IdentityRole("Admin") { Id = "r1" },
            new IdentityRole("Editor") { Id = "r2" }
        };
        roleManager.Setup(x => x.Roles).Returns(roles.AsQueryable());

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.Edit("1");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EditUserViewModel>(viewResult.Model);
        Assert.Equal("1", model.Id);
        Assert.Equal("Test User", model.Name);
        Assert.True(model.EmailConfirmed);
        Assert.Equal(2, model.RoleAssignments.Count);

        var adminAssignment = model.RoleAssignments.Single(r => r.RoleName == "Admin");
        Assert.True(adminAssignment.IsAssigned);

        var editorAssignment = model.RoleAssignments.Single(r => r.RoleName == "Editor");
        Assert.False(editorAssignment.IsAssigned);
    }

    [Fact]
    public async Task Edit_Post_WithInvalidModelState_ReturnsView()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        roleManager.Setup(x => x.Roles).Returns(new List<IdentityRole>().AsQueryable());

        var controller = CreateController(userManager.Object, roleManager.Object);
        controller.ModelState.AddModelError("Name", "Required");

        var model = new EditUserViewModel { Id = "1", Name = "" };

        var result = await controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
    }

    [Fact]
    public async Task Edit_Post_WithValidModel_UpdatesUserAndRedirectsToIndex()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "user@test.local",
            Name = "Old Name",
            EmailConfirmed = false,
            LockoutEnabled = false
        };

        userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        var model = new EditUserViewModel
        {
            Id = "1",
            Email = "user@test.local",
            Name = "New Name",
            EmailConfirmed = true,
            LockoutEnabled = false,
            RoleAssignments = new List<RoleAssignmentViewModel>()
        };

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("New Name", user.Name);
        Assert.True(user.EmailConfirmed);
        userManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Edit_Post_AddsNewRoleAssignment()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "user@test.local",
            Name = "Test User",
            EmailConfirmed = true
        };

        userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        userManager.Setup(x => x.AddToRoleAsync(user, "Editor")).ReturnsAsync(IdentityResult.Success);

        var model = new EditUserViewModel
        {
            Id = "1",
            Email = "user@test.local",
            Name = "Test User",
            EmailConfirmed = true,
            RoleAssignments = new List<RoleAssignmentViewModel>
            {
                new RoleAssignmentViewModel { RoleName = "Editor", IsAssigned = true }
            }
        };

        var controller = CreateController(userManager.Object, roleManager.Object);

        await controller.Edit(model);

        userManager.Verify(x => x.AddToRoleAsync(user, "Editor"), Times.Once);
    }

    [Fact]
    public async Task Edit_Post_RemovesRoleAssignment()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "user@test.local",
            Name = "Test User",
            EmailConfirmed = true
        };

        userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Editor" });
        userManager.Setup(x => x.RemoveFromRoleAsync(user, "Editor")).ReturnsAsync(IdentityResult.Success);

        var model = new EditUserViewModel
        {
            Id = "1",
            Email = "user@test.local",
            Name = "Test User",
            EmailConfirmed = true,
            RoleAssignments = new List<RoleAssignmentViewModel>
            {
                new RoleAssignmentViewModel { RoleName = "Editor", IsAssigned = false }
            }
        };

        var controller = CreateController(userManager.Object, roleManager.Object);

        await controller.Edit(model);

        userManager.Verify(x => x.RemoveFromRoleAsync(user, "Editor"), Times.Once);
    }

    [Fact]
    public async Task Edit_Post_ProtectedUser_CannotRemoveAdminRole()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "admin@test.local",
            Name = "Template Admin",
            EmailConfirmed = true
        };

        userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var model = new EditUserViewModel
        {
            Id = "1",
            Email = "admin@test.local",
            Name = "Template Admin",
            EmailConfirmed = true,
            RoleAssignments = new List<RoleAssignmentViewModel>
            {
                new RoleAssignmentViewModel { RoleName = "Admin", IsAssigned = false }
            }
        };

        var controller = CreateController(userManager.Object, roleManager.Object);

        await controller.Edit(model);

        userManager.Verify(x => x.RemoveFromRoleAsync(user, "Admin"), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_ProtectedUser_CannotDisableEmailConfirmed()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "admin@test.local",
            Name = "Template Admin",
            EmailConfirmed = true
        };

        userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        roleManager.Setup(x => x.Roles).Returns(new List<IdentityRole>().AsQueryable());
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var model = new EditUserViewModel
        {
            Id = "1",
            Email = "admin@test.local",
            Name = "Template Admin",
            EmailConfirmed = false,
            RoleAssignments = new List<RoleAssignmentViewModel>()
        };

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_ProtectedUser_CannotEnableLockout()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var user = new ApplicationUser
        {
            Id = "1",
            Email = "admin@test.local",
            Name = "Template Admin",
            EmailConfirmed = true,
            LockoutEnabled = false
        };

        userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
        roleManager.Setup(x => x.Roles).Returns(new List<IdentityRole>().AsQueryable());
        userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var model = new EditUserViewModel
        {
            Id = "1",
            Email = "admin@test.local",
            Name = "Template Admin",
            EmailConfirmed = true,
            LockoutEnabled = true,
            RoleAssignments = new List<RoleAssignmentViewModel>()
        };

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    private static UsersController CreateController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new AdminBootstrapOptions
        {
            Enabled = true,
            RoleName = "Admin",
            Email = "admin@test.local",
            Password = "Admin123!",
            DisplayName = "Template Admin"
        });

        return new UsersController(userManager, roleManager, options);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();

        return new Mock<RoleManager<IdentityRole>>(
            store.Object,
            null!,
            null!,
            null!,
            null!);
    }
}
