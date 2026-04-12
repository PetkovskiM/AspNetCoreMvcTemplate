using AspNetCoreMvcTemplate.Web.Areas.Admin.Controllers;
using AspNetCoreMvcTemplate.Web.Areas.Admin.ViewModels;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Areas.Admin.Controllers;

public class RolesControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewWithAllRoles_AndMarksProtectedRole()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();

        var roles = new List<IdentityRole>
        {
            new IdentityRole("Admin") { Id = "1" },
            new IdentityRole("Editor") { Id = "2" }
        };

        roleManager.Setup(x => x.Roles).Returns(roles.AsQueryable());

        userManager
            .Setup(x => x.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(new List<ApplicationUser> { new ApplicationUser() });

        userManager
            .Setup(x => x.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser>());

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<RoleListItemViewModel>>(viewResult.Model).ToList();

        Assert.Equal(2, model.Count);

        var admin = model.Single(r => r.Name == "Admin");
        Assert.True(admin.IsProtected);
        Assert.Equal(1, admin.UserCount);

        var editor = model.Single(r => r.Name == "Editor");
        Assert.False(editor.IsProtected);
        Assert.Equal(0, editor.UserCount);
    }

    [Fact]
    public void Create_Get_ReturnsViewWithEmptyModel()
    {
        var controller = CreateController(
            CreateUserManagerMock().Object,
            CreateRoleManagerMock().Object);

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CreateRoleViewModel>(viewResult.Model);
        Assert.Equal(string.Empty, model.Name);
    }

    [Fact]
    public async Task Create_Post_WithInvalidModelState_ReturnsViewWithModel()
    {
        var controller = CreateController(
            CreateUserManagerMock().Object,
            CreateRoleManagerMock().Object);

        controller.ModelState.AddModelError("Name", "Required");
        var model = new CreateRoleViewModel { Name = string.Empty };

        var result = await controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_WithDuplicateName_AddsModelErrorAndReturnsView()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager.Setup(x => x.RoleExistsAsync("Editor")).ReturnsAsync(true);

        var controller = CreateController(CreateUserManagerMock().Object, roleManager.Object);
        var model = new CreateRoleViewModel { Name = "Editor" };

        var result = await controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        roleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task Create_Post_WithValidModel_CreatesRoleAndRedirectsToIndex()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager.Setup(x => x.RoleExistsAsync("Editor")).ReturnsAsync(false);
        roleManager
            .Setup(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == "Editor")))
            .ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(CreateUserManagerMock().Object, roleManager.Object);
        var model = new CreateRoleViewModel { Name = "Editor" };

        var result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        roleManager.Verify(x => x.CreateAsync(It.Is<IdentityRole>(r => r.Name == "Editor")), Times.Once);
    }

    [Fact]
    public async Task Edit_Get_WhenRoleNotFound_ReturnsNotFound()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((IdentityRole?)null);

        var controller = CreateController(CreateUserManagerMock().Object, roleManager.Object);

        var result = await controller.Edit("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsViewWithRoleData()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager
            .Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(new IdentityRole("Editor") { Id = "1" });

        var controller = CreateController(CreateUserManagerMock().Object, roleManager.Object);

        var result = await controller.Edit("1");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EditRoleViewModel>(viewResult.Model);
        Assert.Equal("1", model.Id);
        Assert.Equal("Editor", model.Name);
        Assert.False(model.IsProtected);
    }

    [Fact]
    public async Task Edit_Post_WithProtectedRole_ReturnsViewWithErrorAndDoesNotUpdate()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager
            .Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(new IdentityRole("Admin") { Id = "1" });

        var controller = CreateController(CreateUserManagerMock().Object, roleManager.Object);
        var model = new EditRoleViewModel { Id = "1", Name = "Renamed" };

        var result = await controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.True(model.IsProtected);
        Assert.False(controller.ModelState.IsValid);
        roleManager.Verify(x => x.UpdateAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithValidModel_UpdatesRoleAndRedirectsToIndex()
    {
        var roleManager = CreateRoleManagerMock();
        var role = new IdentityRole("Editor") { Id = "1" };
        roleManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(role);
        roleManager.Setup(x => x.UpdateAsync(role)).ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(CreateUserManagerMock().Object, roleManager.Object);
        var model = new EditRoleViewModel { Id = "1", Name = "Contributor" };

        var result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Contributor", role.Name);
        roleManager.Verify(x => x.UpdateAsync(role), Times.Once);
    }

    [Fact]
    public async Task Delete_Get_WhenRoleNotFound_ReturnsNotFound()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((IdentityRole?)null);

        var controller = CreateController(CreateUserManagerMock().Object, roleManager.Object);

        var result = await controller.Delete("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_WithProtectedRole_DoesNotDelete()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager
            .Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(new IdentityRole("Admin") { Id = "1" });

        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(new List<ApplicationUser>());

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.DeleteConfirmed("1");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DeleteRoleViewModel>(viewResult.Model);
        Assert.True(model.IsProtected);
        Assert.False(controller.ModelState.IsValid);
        roleManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_WithUsersAssigned_DoesNotDelete()
    {
        var roleManager = CreateRoleManagerMock();
        roleManager
            .Setup(x => x.FindByIdAsync("2"))
            .ReturnsAsync(new IdentityRole("Editor") { Id = "2" });

        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser> { new ApplicationUser(), new ApplicationUser() });

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.DeleteConfirmed("2");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DeleteRoleViewModel>(viewResult.Model);
        Assert.Equal(2, model.UserCount);
        Assert.False(controller.ModelState.IsValid);
        roleManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_WithValidRole_DeletesAndRedirectsToIndex()
    {
        var roleManager = CreateRoleManagerMock();
        var role = new IdentityRole("Editor") { Id = "2" };
        roleManager.Setup(x => x.FindByIdAsync("2")).ReturnsAsync(role);
        roleManager.Setup(x => x.DeleteAsync(role)).ReturnsAsync(IdentityResult.Success);

        var userManager = CreateUserManagerMock();
        userManager
            .Setup(x => x.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser>());

        var controller = CreateController(userManager.Object, roleManager.Object);

        var result = await controller.DeleteConfirmed("2");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        roleManager.Verify(x => x.DeleteAsync(role), Times.Once);
    }

    private static RolesController CreateController(
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

        return new RolesController(roleManager, userManager, options);
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
