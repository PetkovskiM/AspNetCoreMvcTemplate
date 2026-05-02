using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Emailing.Models;
using AspNetCoreMvcTemplate.Web.Features;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using AspNetCoreMvcTemplate.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AspNetCoreMvcTemplate.Web.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender emailSender;
        private readonly IFeatureManager featureManager;
        private readonly ILogger<AccountController> logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IFeatureManager featureManager,
            ILogger<AccountController> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailSender = emailSender;
            this.featureManager = featureManager;
            this.logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                logger.LogInformation("User {Email} logged in successfully.", model.Email);

                // IsLocalUrl e security measure za da ne dojde do redirect attacks
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsNotAllowed)
            {
                logger.LogInformation("Login blocked for user {Email} because sign-in is not allowed.", model.Email);

                // TO DO: add other restrictions, make that message more general or smarter.
                ModelState.AddModelError(string.Empty, "Your account is not ready for sign-in yet.");
                ViewBag.ShowResendConfirmationLink = true;

                return View(model);
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("User {Email} is locked out.", model.Email);
                return RedirectToAction(nameof(Lockout));
            }

            logger.LogWarning("Invalid login attempt for user {Email}.", model.Email);

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.Registration))]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.Registration))]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailConfirmationEnabled = featureManager.IsEnabled(nameof(FeatureOptions.EmailConfirmation));

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                // Ako EmailConfirmation feature-ot e isklucen, novite useri vlegvaat
                // direktno bez email confirmation step.
                EmailConfirmed = !emailConfirmationEnabled
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!emailConfirmationEnabled)
                {
                    // Sleep mode na confirmation flow-ot - signiraj se direktno.
                    await signInManager.SignInAsync(user, isPersistent: false);
                    logger.LogInformation("User {Email} registered and signed in (email confirmation disabled).", model.Email);
                    return RedirectToAction("Index", "Home");
                }

                //Create token for email confirmation and send email with the token
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, token }, Request.Scheme);
                if (confirmationLink is null)
                {
                    logger.LogError("Failed to generate confirmation link for user {Email}.", model.Email);

                    ModelState.AddModelError(string.Empty, "Unable to generate email confirmation link.");
                    return View(model);
                }

                //TO DO: Da se napravi htmlBody da se vcituva od file.
                var message = new EmailMessage
                {
                    To = model.Email,
                    Subject = "Welcome to our application!",
                    HtmlBody = $"""
                        <p>Hello {user.Name},</p>
                        <p>Please confirm your account by clicking the link below:</p>
                        <p><a href="{confirmationLink}">Confirm Email</a></p>
                        """
                };

                var sendResult = await emailSender.SendAsync(message);

                if (!sendResult.Succeeded)
                {
                    logger.LogError("Failed to send confirmation email to user {Email}. Error: {ErrorMessage}",
                    model.Email,
                    sendResult.ErrorMessage);

                    ModelState.AddModelError(string.Empty, "We could not send the email right now. Please try again.");
                    return View(model);
                }

                return RedirectToAction(nameof(RegisterConfirmation), new { email = user.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);

        }


        [HttpGet]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.Registration))]
        public IActionResult RegisterConfirmation(string? email)
        {
            ViewBag.Email = email;
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return View("Error");
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                logger.LogWarning("ConfirmEmail failed because user {UserId} was not found.", userId);
                return View("Error");
            }

            var result = await userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                logger.LogInformation("Email confirmed successfully for user {Email}.", user.Email);
                return View();
            }

            logger.LogWarning("Email confirmation failed for user {Email}.", user.Email);
            return View("Error");
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Start na OAuth challenge-ot. ASP.NET Core go redirektira browser-ot do
        // Google/Microsoft, kade userot se avtentikuva. Potoa provajderot go
        // redirektira nazad do ExternalLoginCallback.
        [HttpPost]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.ExternalLogins))]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return BadRequest("Provider not specified.");
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // Callback koj go povikuva provajderot po uspeshen login.
        // Tri sluchai:
        //  1. User-ot vekje go linkuval ovoj provajder -> sign-in
        //  2. Nov user, provajderot prakja email claim -> avtomatski kreiraj lokalen user
        //     so EmailConfirmed = true (provajderot go potvrdil emailot)
        //  3. Nov user bez email claim -> redirektiraj na formata za potvrda
        [HttpGet]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.ExternalLogins))]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                // Detalen error e zapishan samo vo log; korisnikot dobiva generichka poraka
                // za da ne se leakne interna informacija od provajderot vo UI.
                logger.LogWarning("External provider returned error: {Error}", remoteError);
                ModelState.AddModelError(string.Empty, "External login failed. Please try again.");
                return View(nameof(Login), new LoginViewModel { ReturnUrl = returnUrl });
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                logger.LogWarning("GetExternalLoginInfoAsync returned null; external login context lost.");
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            // Sluchai 1: vekje linkuval - sign in.
            // bypassTwoFactor: true e default-ot na Microsoft Identity scaffoldot. Vo
            // ovoj template 2FA ne e implementiran, taka shto vrednosta nema efekt.
            // Koga kje se dodade 2FA branch, treba da se prefrli na false i da se
            // dodade LoginWith2fa action za da se zavrshi 2FA flow-ot.
            var signInResult = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                logger.LogInformation(
                    "User signed in via {Provider} (ProviderKey: {ProviderKey}).",
                    info.LoginProvider,
                    info.ProviderKey);
                return RedirectToLocal(returnUrl);
            }

            if (signInResult.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }

            // Sluchai 2 ili 3: prv pat login so ovoj provajder za ovoj user.
            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                // Sluchaj 3: provajderot ne dal email - pokazi forma za vnesuvanje
                var model = new ExternalLoginConfirmationViewModel
                {
                    ReturnUrl = returnUrl,
                    ProviderDisplayName = info.ProviderDisplayName
                };
                return View(nameof(ExternalLoginConfirmation), model);
            }

            // Sluchaj 2: imame email od provajderot, avtomatski kreiraj user.
            return await CreateExternalUserAndSignInAsync(info, email, returnUrl);
        }

        // Fallback za Sluchaj 3 - userot rachno vnese email.
        [HttpPost]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.ExternalLogins))]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                logger.LogWarning("ExternalLoginConfirmation: external login info was lost.");
                return RedirectToAction(nameof(Login));
            }

            return await CreateExternalUserAndSignInAsync(info, model.Email, model.ReturnUrl);
        }

        private async Task<IActionResult> CreateExternalUserAndSignInAsync(
            ExternalLoginInfo info,
            string email,
            string? returnUrl)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                // Postoi lokalen user so ovoj email no ne e linkuvan so ovoj provajder.
                // Za bezbednost ne dozvoluvame avtomatsko linkuvanje - userot mora
                // prvo da se najavi so password pa da go linkuva od profilot.
                logger.LogInformation(
                    "External login with existing email {Email} but no link. User must link from profile.",
                    email);

                ModelState.AddModelError(string.Empty,
                    "An account with this email already exists. Please sign in with your password first and link the external provider from your profile.");
                return View(nameof(Login), new LoginViewModel { Email = email, ReturnUrl = returnUrl });
            }

            // name claim e korisno da se zadrzi ako provajderot go dal
            var name =
            info.Principal.FindFirst(ClaimTypes.GivenName)?.Value
            ?? info.Principal.FindFirst(ClaimTypes.Name)?.Value
            ?? email;

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name,
                EmailConfirmed = true // provajderot ja potvrdil email adresata
            };

            var createResult = await userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(nameof(Login), new LoginViewModel { Email = email, ReturnUrl = returnUrl });
            }

            var linkResult = await userManager.AddLoginAsync(newUser, info);
            if (!linkResult.Succeeded)
            {
                logger.LogError(
                    "Failed to link external login for {Email}: {Errors}",
                    email,
                    string.Join("; ", linkResult.Errors.Select(e => e.Description)));

                foreach (var error in linkResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(nameof(Login), new LoginViewModel { Email = email, ReturnUrl = returnUrl });
            }

            await signInManager.SignInAsync(newUser, isPersistent: false);
            logger.LogInformation("Created local user {Email} from external provider {Provider}.", email, info.LoginProvider);
            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new EmailInputViewModel());
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(EmailInputViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
            {
                logger.LogInformation("ForgotPassword requested for non-existing or unconfirmed email {Email}.", model.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { token, email = user.Email },
                Request.Scheme);

            if (resetLink is null)
            {
                logger.LogError("Failed to generate reset password link for user {Email}.", user.Email);
                ModelState.AddModelError(string.Empty, "Unable to generate password reset link.");
                return View(model);
            }

            //TODO: Da se napravi htmlBody da se vcituva od file.
            var sendResult = await emailSender.SendAsync(new EmailMessage
            {
                To = user.Email!,
                Subject = "Reset your password",
                HtmlBody = $"""
                    <p>Hello {user.Name},</p>
                    <p>You can reset your password by clicking the link below:</p>
                    <p><a href="{resetLink}">Reset Password</a></p>
                    """
            });

            if (!sendResult.Succeeded)
            {
                logger.LogError("Failed to send reset password email to {Email}. Error: {ErrorMessage}",
                user.Email,
                sendResult.ErrorMessage);

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            logger.LogInformation("Password reset email sent to {Email}.", user.Email);
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                return View("Error");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                logger.LogWarning("ResetPassword attempted for non-existing email {Email}.", model.Email);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                logger.LogInformation("Password reset succeeded for user {Email}.", user.Email);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            logger.LogWarning("Password reset failed for user {Email}.", user.Email);

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.EmailConfirmation))]
        public IActionResult ResendConfirmationEmail(string? email = null)
        {
            var model = new EmailInputViewModel
            {
                Email = email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [FeatureGate(nameof(FeatureOptions.EmailConfirmation))]
        //Napraven e ovoj endpoint so namera da se koristi i za resend confirmation email. Ne se koristi za forgot password zatoa sto za forgot password ne e potrebno da se proveruva dali email e confirmed, a i tokenot e razlicen.
        public async Task<IActionResult> ResendConfirmationEmail(EmailInputViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                logger.LogInformation("ResendConfirmationEmail requested for non-existing email {Email}.", model.Email);
                return RedirectToAction(nameof(ResendConfirmationEmailConfirmation));
            }

            if (await userManager.IsEmailConfirmedAsync(user))
            {
                logger.LogInformation("ResendConfirmationEmail requested for already confirmed email {Email}.", model.Email);
                return RedirectToAction(nameof(ResendConfirmationEmailConfirmation));
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token },
                Request.Scheme);

            if (confirmationLink is null)
            {
                logger.LogError("Failed to generate resend confirmation link for user {Email}.", user.Email);
                ModelState.AddModelError(string.Empty, "Unable to generate email confirmation link.");
                return View(model);
            }

            var sendResult = await emailSender.SendAsync(new EmailMessage
            {
                To = user.Email!,
                Subject = "Confirm your email",
                HtmlBody = $"""
                <p>Hello {user.Name},</p>
                <p>Please confirm your account by clicking the link below:</p>
                <p><a href="{confirmationLink}">Confirm Email</a></p>
                """
            });

            if (!sendResult.Succeeded)
            {
                logger.LogError("Failed to resend confirmation email to {Email}. Error: {ErrorMessage}",
                    user.Email,
                    sendResult.ErrorMessage);

                return RedirectToAction(nameof(ResendConfirmationEmailConfirmation));
            }

            logger.LogInformation("Confirmation email resent to {Email}.", user.Email);

            return RedirectToAction(nameof(ResendConfirmationEmailConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendConfirmationEmailConfirmation()
        {
            return View();
        }
    }
}


