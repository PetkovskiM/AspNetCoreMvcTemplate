using System.Security.Claims;
using AspNetCoreMvcTemplate.Web.Authorization;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Authorization;

public class ApplicationUserClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task GenerateClaims_WhenEmailConfirmed_AddsEmailVerifiedTrueClaim()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "user@test.local",
            Email = "user@test.local",
            Name = "Test User",
            EmailConfirmed = true
        };
        var factory = CreateFactory(user);

        var identity = await factory.InvokeGenerateClaimsAsync(user);

        Assert.Contains(identity.Claims, c => c.Type == "email_verified" && c.Value == "true");
    }

    [Fact]
    public async Task GenerateClaims_WhenEmailNotConfirmed_AddsEmailVerifiedFalseClaim()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "user@test.local",
            Email = "user@test.local",
            Name = "Test User",
            EmailConfirmed = false
        };
        var factory = CreateFactory(user);

        var identity = await factory.InvokeGenerateClaimsAsync(user);

        Assert.Contains(identity.Claims, c => c.Type == "email_verified" && c.Value == "false");
    }

    [Fact]
    public async Task GenerateClaims_WhenNameIsSet_AddsNameClaim()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "user@test.local",
            Email = "user@test.local",
            Name = "Mile Petkovski",
            EmailConfirmed = true
        };
        var factory = CreateFactory(user);

        var identity = await factory.InvokeGenerateClaimsAsync(user);

        Assert.Contains(identity.Claims, c => c.Type == "name" && c.Value == "Mile Petkovski");
    }

    [Fact]
    public async Task GenerateClaims_WhenNameIsEmpty_DoesNotAddNameClaim()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "user@test.local",
            Email = "user@test.local",
            Name = string.Empty,
            EmailConfirmed = true
        };
        var factory = CreateFactory(user);

        var identity = await factory.InvokeGenerateClaimsAsync(user);

        Assert.DoesNotContain(identity.Claims, c => c.Type == "name");
    }

    private static TestableFactory CreateFactory(ApplicationUser user)
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserIdAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(user.Id);
        userManager.Setup(x => x.GetUserNameAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(user.UserName);
        userManager.Setup(x => x.SupportsUserEmail).Returns(true);
        userManager.Setup(x => x.GetEmailAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(user.Email);
        userManager.Setup(x => x.SupportsUserSecurityStamp).Returns(false);
        userManager.Setup(x => x.SupportsUserClaim).Returns(false);

        var roleManager = CreateRoleManagerMock();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());

        return new TestableFactory(userManager.Object, roleManager.Object, options);
    }

    // Otvora pristap do protected GenerateClaimsAsync za testovi.
    private class TestableFactory : ApplicationUserClaimsPrincipalFactory
    {
        public TestableFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options)
        {
        }

        public Task<ClaimsIdentity> InvokeGenerateClaimsAsync(ApplicationUser user)
            => GenerateClaimsAsync(user);
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
