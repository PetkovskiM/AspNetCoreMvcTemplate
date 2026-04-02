using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.Options;
using AspNetCoreMvcTemplate.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Services;

public class AdminBootstrapSeederTests
{
    [Fact]
    public async Task SeedAsync_DoesNothing_WhenBootstrapIsDisabled()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var options = Microsoft.Extensions.Options.Options.Create(new AdminBootstrapOptions { Enabled = false });
        var seeder = CreateSeeder(userManager.Object, roleManager.Object, options);

        await seeder.SeedAsync();

        roleManager.Verify(x => x.RoleExistsAsync(It.IsAny<string>()), Times.Never);
        userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_CreatesRoleAndUser_AndAssignsUserToRole()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var options = CreateEnabledOptions();
        ApplicationUser? createdUser = null;

        roleManager
            .Setup(x => x.RoleExistsAsync(options.Value.RoleName))
            .ReturnsAsync(false);

        roleManager
            .Setup(x => x.CreateAsync(It.Is<IdentityRole>(role => role.Name == options.Value.RoleName)))
            .ReturnsAsync(IdentityResult.Success);

        userManager
            .Setup(x => x.FindByEmailAsync(options.Value.Email))
            .ReturnsAsync((ApplicationUser?)null);

        userManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), options.Value.Password))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);

        userManager
            .Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), options.Value.RoleName))
            .ReturnsAsync(false);

        userManager
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), options.Value.RoleName))
            .ReturnsAsync(IdentityResult.Success);

        var seeder = CreateSeeder(userManager.Object, roleManager.Object, options);

        await seeder.SeedAsync();

        Assert.NotNull(createdUser);
        Assert.Equal(options.Value.Email, createdUser.Email);
        Assert.Equal(options.Value.Email, createdUser.UserName);
        Assert.Equal(options.Value.DisplayName, createdUser.Name);
        Assert.True(createdUser.EmailConfirmed);

        roleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Once);
        userManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), options.Value.Password), Times.Once);
        userManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), options.Value.RoleName), Times.Once);
        userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_UpdatesExistingUser_AndSkipsRoleCreation_WhenAlreadyPresent()
    {
        var userManager = CreateUserManagerMock();
        var roleManager = CreateRoleManagerMock();
        var options = CreateEnabledOptions();
        var existingUser = new ApplicationUser
        {
            Email = options.Value.Email,
            UserName = options.Value.Email,
            Name = "Old Name",
            EmailConfirmed = false
        };

        roleManager
            .Setup(x => x.RoleExistsAsync(options.Value.RoleName))
            .ReturnsAsync(true);

        userManager
            .Setup(x => x.FindByEmailAsync(options.Value.Email))
            .ReturnsAsync(existingUser);

        userManager
            .Setup(x => x.UpdateAsync(existingUser))
            .ReturnsAsync(IdentityResult.Success);

        userManager
            .Setup(x => x.IsInRoleAsync(existingUser, options.Value.RoleName))
            .ReturnsAsync(true);

        var seeder = CreateSeeder(userManager.Object, roleManager.Object, options);

        await seeder.SeedAsync();

        Assert.Equal(options.Value.DisplayName, existingUser.Name);
        Assert.True(existingUser.EmailConfirmed);

        roleManager.Verify(x => x.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        userManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(x => x.UpdateAsync(existingUser), Times.Once);
        userManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    private static AdminBootstrapSeeder CreateSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<AdminBootstrapOptions> options)
    {
        return new AdminBootstrapSeeder(
            userManager,
            roleManager,
            options,
            NullLogger<AdminBootstrapSeeder>.Instance);
    }

    private static IOptions<AdminBootstrapOptions> CreateEnabledOptions()
    {
        return Microsoft.Extensions.Options.Options.Create(new AdminBootstrapOptions
        {
            Enabled = true,
            RoleName = "Admin",
            Email = "admin@test.local",
            Password = "Admin123!",
            DisplayName = "Template Admin"
        });
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
