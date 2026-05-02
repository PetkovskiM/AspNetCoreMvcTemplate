using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Web.Controllers;
using AspNetCoreMvcTemplate.Web.Features;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.ViewModels.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Controllers;

public class AccountControllerTests
{
    [Fact]
    public async Task ForgotPassword_Post_RedirectsToConfirmation_WhenUserDoesNotExist()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        userManager
            .Setup(x => x.FindByEmailAsync("missing@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var model = new EmailInputViewModel
        {
            Email = "missing@test.com"
        };

        // Act
        var result = await controller.ForgotPassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ForgotPasswordConfirmation), redirectResult.ActionName);
    }

    [Fact]
    public async Task ForgotPassword_Post_RedirectsToConfirmation_WhenEmailIsNotConfirmed()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = new ApplicationUser
        {
            Email = "user@test.com",
            UserName = "user@test.com",
            Name = "Test User"
        };

        userManager
            .Setup(x => x.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(user);

        userManager
            .Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var model = new EmailInputViewModel
        {
            Email = "user@test.com"
        };

        // Act
        var result = await controller.ForgotPassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ForgotPasswordConfirmation), redirectResult.ActionName);
    }

    [Fact]
    public async Task ResendConfirmationEmail_Post_RedirectsToConfirmation_WhenUserDoesNotExist()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        userManager
            .Setup(x => x.FindByEmailAsync("missing@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var model = new EmailInputViewModel
        {
            Email = "missing@test.com"
        };

        // Act
        var result = await controller.ResendConfirmationEmail(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ResendConfirmationEmailConfirmation), redirectResult.ActionName);
    }

    [Fact]
    public async Task Login_Post_RedirectsToLockout_WhenUserIsLockedOut()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        signInManager
            .Setup(x => x.PasswordSignInAsync("mile123", "password", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var model = new LoginViewModel
        {
            Email = "mile123",
            Password = "password",
            RememberMe = false
        };

        // Act
        var result = await controller.Login(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.Lockout), redirectResult.ActionName);
    }

    [Fact]
    public async Task ResetPassword_Post_RedirectsToConfirmation_WhenUserDoesNotExist()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        userManager
            .Setup(x => x.FindByEmailAsync("missing@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        var model = new ResetPasswordViewModel
        {
            Email = "missing@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            Token = "some-token"
        };

        // Act
        var result = await controller.ResetPassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AccountController.ResetPasswordConfirmation), redirectResult.ActionName);
    }


    [Fact]
    public async Task ResetPassword_Post_ReturnsView_WhenModelStateIsInvalid()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);
        controller.ModelState.AddModelError("Password", "Password is required.");

        var model = new ResetPasswordViewModel
        {
            Email = "user@test.com",
            Password = "",
            ConfirmPassword = "",
            Token = "some-token"
        };

        // Act
        var result = await controller.ResetPassword(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
    }

    [Fact]
    public async Task ConfirmEmail_Get_ReturnsError_WhenUserIsNotFound()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        userManager
            .Setup(x => x.FindByIdAsync("missing-user-id"))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        // Act
        var result = await controller.ConfirmEmail("missing-user-id", "some-token");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", viewResult.ViewName);
    }


    [Fact]
    public async Task ConfirmEmail_Get_ReturnsView_WhenConfirmationSucceeds()
    {
        // Arrange
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var emailSender = new Mock<IEmailSender>();

        var user = new ApplicationUser
        {
            Id = "user-id",
            Email = "user@test.com",
            UserName = "user@test.com",
            Name = "Test User"
        };

        userManager
            .Setup(x => x.FindByIdAsync("user-id"))
            .ReturnsAsync(user);

        userManager
            .Setup(x => x.ConfirmEmailAsync(user, "valid-token"))
            .ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(userManager.Object, signInManager.Object, emailSender.Object);

        // Act
        var result = await controller.ConfirmEmail("user-id", "valid-token");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName); // default view
    }


    private static AccountController CreateController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender)
    {
        // Default: site features se vkluceni - tako site postojni testovi
        // se odnesuvaat kako i pred dodavanjeto na feature toggles.
        var featureManager = new Mock<IFeatureManager>();
        featureManager.Setup(x => x.IsEnabled(It.IsAny<string>())).Returns(true);

        return new AccountController(
            userManager,
            signInManager,
            emailSender,
            featureManager.Object,
            NullLogger<AccountController>.Instance);
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