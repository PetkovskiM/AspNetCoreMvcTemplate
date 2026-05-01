using System.Security.Claims;
using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Emailing.Models;
using AspNetCoreMvcTemplate.Web.Controllers;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.ViewModels.Profile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Controllers;

public class ProfileControllerTests
{
    [Fact]
    public async Task ChangePassword_Post_ReturnsView_WhenCurrentPasswordIsWrong()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager
            .Setup(x => x.ChangePasswordAsync(user, "wrong", "Newpass1"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ChangePassword(new ChangePasswordViewModel
        {
            CurrentPassword = "wrong",
            NewPassword = "Newpass1",
            ConfirmPassword = "Newpass1"
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task ChangePassword_Post_RedirectsToIndex_WhenSuccessful()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager
            .Setup(x => x.ChangePasswordAsync(user, "Old1", "New1"))
            .ReturnsAsync(IdentityResult.Success);

        signInManager
            .Setup(x => x.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ChangePassword(new ChangePasswordViewModel
        {
            CurrentPassword = "Old1",
            NewPassword = "New1",
            ConfirmPassword = "New1"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProfileController.Index), redirect.ActionName);
        signInManager.Verify(x => x.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task SetPassword_Get_RedirectsToChangePassword_WhenUserAlreadyHasPassword()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.SetPassword();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProfileController.ChangePassword), redirect.ActionName);
    }

    [Fact]
    public async Task SetPassword_Post_AddsPassword_WhenUserHasNoPassword()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);
        userManager
            .Setup(x => x.AddPasswordAsync(user, "Newpass1"))
            .ReturnsAsync(IdentityResult.Success);

        signInManager.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.SetPassword(new SetPasswordViewModel
        {
            NewPassword = "Newpass1",
            ConfirmPassword = "Newpass1"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProfileController.Index), redirect.ActionName);
        userManager.Verify(x => x.AddPasswordAsync(user, "Newpass1"), Times.Once);
    }

    [Fact]
    public async Task ChangeEmail_Post_SendsConfirmationToNewAddress()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager
            .Setup(x => x.GenerateChangeEmailTokenAsync(user, "new@test.com"))
            .ReturnsAsync("test-token");

        EmailMessage? captured = null;
        emailSender
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync(new EmailSendResult { Succeeded = true });

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ChangeEmail(new ChangeEmailViewModel
        {
            NewEmail = "new@test.com"
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(captured);
        Assert.Equal("new@test.com", captured!.To);
    }

    [Fact]
    public async Task ChangeEmail_Post_AddsModelError_WhenNewEmailEqualsCurrent()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ChangeEmail(new ChangeEmailViewModel
        {
            NewEmail = user.Email!
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        emailSender.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmEmailChange_Get_UpdatesEmailAndUserName_WhenTokenIsValid()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager
            .Setup(x => x.ChangeEmailAsync(user, "new@test.com", "valid-token"))
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(x => x.SetUserNameAsync(user, "new@test.com"))
            .ReturnsAsync(IdentityResult.Success);

        signInManager.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ConfirmEmailChange(user.Id, "new@test.com", "valid-token");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProfileController.Index), redirect.ActionName);
        userManager.Verify(x => x.SetUserNameAsync(user, "new@test.com"), Times.Once);
    }

    [Fact]
    public async Task RemoveLogin_Refuses_WhenWouldOrphanAccount()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);
        userManager
            .Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>
            {
                new("Google", "key-1", "Google")
            });

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.RemoveLogin("Google", "key-1");

        Assert.IsType<RedirectToActionResult>(result);
        userManager.Verify(
            x => x.RemoveLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveLogin_Succeeds_WhenUserHasPassword()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        userManager
            .Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>
            {
                new("Google", "key-1", "Google")
            });
        userManager
            .Setup(x => x.RemoveLoginAsync(user, "Google", "key-1"))
            .ReturnsAsync(IdentityResult.Success);
        signInManager.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.RemoveLogin("Google", "key-1");

        Assert.IsType<RedirectToActionResult>(result);
        userManager.Verify(x => x.RemoveLoginAsync(user, "Google", "key-1"), Times.Once);
    }

    [Fact]
    public async Task ChangeName_Post_UpdatesUserAndRefreshesSignIn()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = CreateUser();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        signInManager.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.ChangeName(new ChangeNameViewModel { Name = "New Name" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProfileController.Index), redirect.ActionName);
        Assert.Equal("New Name", user.Name);
        signInManager.Verify(x => x.RefreshSignInAsync(user), Times.Once);
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@test.com",
            Email = "user@test.com",
            Name = "Test User"
        };
    }

    private static ProfileController CreateController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender)
    {
        var controller = new ProfileController(
            userManager,
            signInManager,
            emailSender,
            NullLogger<ProfileController>.Instance);

        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Url helper za Url.Action vo ChangeEmail.
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/Profile/ConfirmEmailChange");
        controller.Url = urlHelper.Object;

        // TempData treba da postoi za TempData["..."] da raboti.
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            controller.HttpContext,
            new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>().Object);

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
