using System.Security.Claims;
using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Web.Controllers;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.ViewModels.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Controllers;

public class AccountControllerExternalLoginTests
{
    [Fact]
    public async Task ExternalLoginCallback_ReturnsLoginView_WhenRemoteErrorIsPresent()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ExternalLoginCallback(returnUrl: null, remoteError: "access_denied");

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(nameof(AccountController.Login), viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task ExternalLoginCallback_RedirectsToLogin_WhenExternalLoginInfoIsNull()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        signInManager
            .Setup(x => x.GetExternalLoginInfoAsync(null))
            .ReturnsAsync((ExternalLoginInfo?)null);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ExternalLoginCallback();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.Login), redirect.ActionName);
    }

    [Fact]
    public async Task ExternalLoginCallback_RedirectsHome_WhenUserAlreadyLinked()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var info = CreateExternalLoginInfo("user@test.com", "Test User", provider: "Google");

        signInManager
            .Setup(x => x.GetExternalLoginInfoAsync(null))
            .ReturnsAsync(info);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "provider-key", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ExternalLoginCallback();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ExternalLoginCallback_CreatesUserWithEmailConfirmedTrue_WhenProviderReturnsEmail()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var info = CreateExternalLoginInfo("new@test.com", "New User", provider: "Google");

        signInManager
            .Setup(x => x.GetExternalLoginInfoAsync(null))
            .ReturnsAsync(info);

        signInManager
            .Setup(x => x.ExternalLoginSignInAsync("Google", "provider-key", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        userManager
            .Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? createdUser = null;
        userManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => createdUser = u)
            .ReturnsAsync(IdentityResult.Success);

        userManager
            .Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), info))
            .ReturnsAsync(IdentityResult.Success);

        signInManager
            .Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ExternalLoginCallback();

        Assert.NotNull(createdUser);
        Assert.Equal("new@test.com", createdUser!.Email);
        Assert.True(createdUser.EmailConfirmed);
        Assert.Equal("New User", createdUser.Name);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ExternalLoginConfirmation_Post_CreatesUserAndSignsIn_WhenValid()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var info = CreateExternalLoginInfo(email: null, name: null, provider: "Microsoft");

        signInManager
            .Setup(x => x.GetExternalLoginInfoAsync(null))
            .ReturnsAsync(info);

        userManager
            .Setup(x => x.FindByEmailAsync("manual@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        ApplicationUser? createdUser = null;
        userManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => createdUser = u)
            .ReturnsAsync(IdentityResult.Success);

        userManager
            .Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), info))
            .ReturnsAsync(IdentityResult.Success);

        signInManager
            .Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var model = new ExternalLoginConfirmationViewModel
        {
            Email = "manual@test.com"
        };

        var result = await controller.ExternalLoginConfirmation(model);

        Assert.NotNull(createdUser);
        Assert.Equal("manual@test.com", createdUser!.Email);
        Assert.True(createdUser.EmailConfirmed);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    private static ExternalLoginInfo CreateExternalLoginInfo(string? email, string? name, string provider)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }
        if (!string.IsNullOrEmpty(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        var identity = new ClaimsIdentity(claims, provider);
        var principal = new ClaimsPrincipal(identity);

        return new ExternalLoginInfo(principal, provider, "provider-key", provider);
    }

    private static AccountController CreateController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender)
    {
        var controller = new AccountController(
            userManager,
            signInManager,
            emailSender,
            NullLogger<AccountController>.Instance);

        // URL helper e potreben za Url.Action i Url.IsLocalUrl vo actionite.
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(false);
        urlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/Account/ExternalLoginCallback");
        controller.Url = urlHelper.Object;

        return controller;
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

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            contextAccessor.Object,
            claimsFactory.Object,
            null!,
            null!,
            null!,
            null!);
    }
}
