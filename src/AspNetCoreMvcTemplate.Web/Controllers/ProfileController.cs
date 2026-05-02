using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Emailing.Models;
using AspNetCoreMvcTemplate.Web.Features;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.ViewModels.Profile;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreMvcTemplate.Web.Controllers
{
    // Self-service profile actions. Site dejstva baraat avtenticiran user.
    [Authorize]
    [AutoValidateAntiforgeryToken]
    [FeatureGate(nameof(FeatureOptions.ProfileManagement))]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender emailSender;
        private readonly ILogger<ProfileController> logger;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<ProfileController> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailSender = emailSender;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var currentLogins = await userManager.GetLoginsAsync(user);
            var allSchemes = await signInManager.GetExternalAuthenticationSchemesAsync();

            // Provajderi koi se konfigurirani vo aplikacijata, no ne se ushte
            // povrzani so ovoj user - tie se kandidati za "Link" butonot.
            var availableProviders = allSchemes
                .Where(s => currentLogins.All(l => l.LoginProvider != s.Name))
                .ToList();

            var model = new ProfileIndexViewModel
            {
                Name = user.Name,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                HasPassword = await userManager.HasPasswordAsync(user),
                CurrentLogins = currentLogins,
                AvailableProviders = availableProviders,
                StatusMessage = TempData["StatusMessage"] as string
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeName()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            return View(new ChangeNameViewModel { Name = user.Name });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeName(ChangeNameViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            user.Name = model.Name;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            // Refresh za da se obnovi `name` claim-ot vo cookie-to (factory-to go emituva).
            await signInManager.RefreshSignInAsync(user);
            logger.LogInformation("User {Email} changed their name.", user.Email);

            TempData["StatusMessage"] = "Your name has been updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            // Useri bez lokalen password odat na SetPassword.
            if (!await userManager.HasPasswordAsync(user))
            {
                return RedirectToAction(nameof(SetPassword));
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            await signInManager.RefreshSignInAsync(user);
            logger.LogInformation("User {Email} changed their password.", user.Email);

            TempData["StatusMessage"] = "Your password has been changed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SetPassword()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            // Ako vekje ima lozinka, kanal e ChangePassword - ne SetPassword.
            if (await userManager.HasPasswordAsync(user))
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            if (await userManager.HasPasswordAsync(user))
            {
                // Race-condition guard - nekoj drug tab vekje postavil lozinka.
                return RedirectToAction(nameof(ChangePassword));
            }

            var result = await userManager.AddPasswordAsync(user, model.NewPassword);
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            await signInManager.RefreshSignInAsync(user);
            logger.LogInformation("User {Email} set a local password.", user.Email);

            TempData["StatusMessage"] = "Your password has been set. You can now sign in with your email and password.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangeEmail()
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            return View(new ChangeEmailViewModel { CurrentEmail = user.Email ?? string.Empty });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            // Vrakjame current email vo modelot za da go ima vo view-ot pri error.
            model.CurrentEmail = user.Email ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.Equals(model.NewEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.NewEmail), "The new email is the same as your current email.");
                return View(model);
            }

            // Tokenot se generira so noviot email kako payload.
            var token = await userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);

            var confirmationLink = Url.Action(
                nameof(ConfirmEmailChange),
                "Profile",
                new { userId = user.Id, newEmail = model.NewEmail, token },
                Request.Scheme);

            if (confirmationLink is null)
            {
                logger.LogError("Failed to generate change-email link for user {Email}.", user.Email);
                ModelState.AddModelError(string.Empty, "Unable to generate the confirmation link.");
                return View(model);
            }

            var sendResult = await emailSender.SendAsync(new EmailMessage
            {
                To = model.NewEmail,
                Subject = "Confirm your new email",
                HtmlBody = $"""
                    <p>Hello {user.Name},</p>
                    <p>Please confirm your new email address by clicking the link below:</p>
                    <p><a href="{confirmationLink}">Confirm new email</a></p>
                    <p>If you did not request this change, you can ignore this message.</p>
                    """
            });

            if (!sendResult.Succeeded)
            {
                logger.LogError("Failed to send change-email confirmation to {NewEmail}. Error: {Error}",
                    model.NewEmail,
                    sendResult.ErrorMessage);
                ModelState.AddModelError(string.Empty, "We could not send the confirmation email right now. Please try again.");
                return View(model);
            }

            logger.LogInformation("Change-email confirmation sent for user {OldEmail} -> {NewEmail}.", user.Email, model.NewEmail);

            TempData["StatusMessage"] = $"A confirmation link has been sent to {model.NewEmail}. Click it to complete the change.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmailChange(string userId, string newEmail, string token)
        {
            if (string.IsNullOrWhiteSpace(userId)
                || string.IsNullOrWhiteSpace(newEmail)
                || string.IsNullOrWhiteSpace(token))
            {
                return View("Error");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                logger.LogWarning("ConfirmEmailChange failed because user {UserId} was not found.", userId);
                return View("Error");
            }

            var changeResult = await userManager.ChangeEmailAsync(user, newEmail, token);
            if (!changeResult.Succeeded)
            {
                logger.LogWarning("ChangeEmailAsync failed for user {UserId}.", userId);
                return View("Error");
            }

            // UserName mora da se sinhronizira so noviot email - inaku PasswordSignInAsync
            // (koja koristi UserName) ke pravi mismatch i userot ne moze da se najavi
            // so noviot email.
            var setUserNameResult = await userManager.SetUserNameAsync(user, newEmail);
            if (!setUserNameResult.Succeeded)
            {
                logger.LogError("SetUserNameAsync failed after email change for user {UserId}.", userId);
                return View("Error");
            }

            await signInManager.RefreshSignInAsync(user);
            logger.LogInformation("Email changed successfully for user {UserId} to {NewEmail}.", userId, newEmail);

            TempData["StatusMessage"] = "Your email has been updated.";
            return RedirectToAction(nameof(Index));
        }

        // start na OAuth flow-ot za povrzuvanje na ekstern provajder
        // so vekje avtenticiran user.
        [HttpPost]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return BadRequest("Provider not specified.");
            }

            // Ja chistime postojnata external cookie za da ne se izmesa so
            // novo-stignatite informacii od provajderot.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = Url.Action(nameof(LinkLoginCallback), "Profile");
            var userId = userManager.GetUserId(User);
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userId);

            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback(string? remoteError = null)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            // OnRemoteFailure (vidi AuthenticationServiceCollectionExtensions) gi konvertira
            // OAuth cancel/error vo ovoj query param namesto unhandled exception.
            if (remoteError != null)
            {
                logger.LogInformation("LinkLogin cancelled or failed for user {UserId}: {Error}", user.Id, remoteError);
                TempData["StatusMessage"] = "Linking was cancelled or failed. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            // Vazno: predavame userId - taka GetExternalLoginInfoAsync go zema
            // vlezniot kontekst za toj specifichen user.
            var info = await signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info is null)
            {
                logger.LogWarning("LinkLoginCallback: external login info was lost for user {UserId}.", user.Id);
                TempData["StatusMessage"] = "Could not link the external account. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            var result = await userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                logger.LogWarning("AddLoginAsync failed for user {UserId} provider {Provider}.", user.Id, info.LoginProvider);
                TempData["StatusMessage"] = "Could not link the external account. It may already be linked to another user.";
                return RedirectToAction(nameof(Index));
            }

            // Cleanup - zatvori ja external cookie-to koja ja koristevme samo za linking.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            logger.LogInformation("User {UserId} linked external provider {Provider}.", user.Id, info.LoginProvider);
            TempData["StatusMessage"] = $"The {info.ProviderDisplayName} account has been linked.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            if (string.IsNullOrWhiteSpace(loginProvider) || string.IsNullOrWhiteSpace(providerKey))
            {
                return BadRequest();
            }

            var user = await userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            // Orphan-prevention: userot ne smee da ostane bez nachin za sign-in.
            // Mora da ima ili lozinka ili barem ushte eden ekstern provajder.
            var hasPassword = await userManager.HasPasswordAsync(user);
            var otherLogins = (await userManager.GetLoginsAsync(user))
                .Count(l => l.LoginProvider != loginProvider);

            if (!hasPassword && otherLogins == 0)
            {
                TempData["StatusMessage"] = "You must set a password or link another provider before unlinking your only sign-in method.";
                return RedirectToAction(nameof(Index));
            }

            var result = await userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                logger.LogWarning("RemoveLoginAsync failed for user {UserId} provider {Provider}.", user.Id, loginProvider);
                TempData["StatusMessage"] = "Could not unlink the external account.";
                return RedirectToAction(nameof(Index));
            }

            await signInManager.RefreshSignInAsync(user);
            logger.LogInformation("User {UserId} unlinked external provider {Provider}.", user.Id, loginProvider);

            TempData["StatusMessage"] = $"The {loginProvider} account has been unlinked.";
            return RedirectToAction(nameof(Index));
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
