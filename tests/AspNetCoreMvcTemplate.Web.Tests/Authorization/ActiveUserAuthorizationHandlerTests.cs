using System.Security.Claims;
using AspNetCoreMvcTemplate.Web.Authorization;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Authorization;

public class ActiveUserAuthorizationHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_DoesNotSucceed()
    {
        var userManager = CreateUserManagerMock();
        var handler = new ActiveUserAuthorizationHandler(userManager.Object);
        var requirement = new ActiveUserRequirement();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity()); // neautentikuvan
        var context = new AuthorizationHandlerContext(new[] { requirement }, anonymous, resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_WhenUserManagerReturnsNull_DoesNotSucceed()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

        var handler = new ActiveUserAuthorizationHandler(userManager.Object);
        var requirement = new ActiveUserRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, CreateAuthenticatedUser(), resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_DoesNotSucceed()
    {
        var user = new ApplicationUser { Id = "1", EmailConfirmed = false };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var handler = new ActiveUserAuthorizationHandler(userManager.Object);
        var requirement = new ActiveUserRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, CreateAuthenticatedUser(), resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_WhenLockoutEndInFuture_DoesNotSucceed()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            EmailConfirmed = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var handler = new ActiveUserAuthorizationHandler(userManager.Object);
        var requirement = new ActiveUserRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, CreateAuthenticatedUser(), resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_WhenEmailConfirmedAndNotLockedOut_Succeeds()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            EmailConfirmed = true,
            LockoutEnd = null
        };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var handler = new ActiveUserAuthorizationHandler(userManager.Object);
        var requirement = new ActiveUserRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, CreateAuthenticatedUser(), resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_WhenLockoutEndInPast_Succeeds()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            EmailConfirmed = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-10) // istechen lockout
        };
        var userManager = CreateUserManagerMock();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var handler = new ActiveUserAuthorizationHandler(userManager.Object);
        var requirement = new ActiveUserRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, CreateAuthenticatedUser(), resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreateAuthenticatedUser()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
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
}
