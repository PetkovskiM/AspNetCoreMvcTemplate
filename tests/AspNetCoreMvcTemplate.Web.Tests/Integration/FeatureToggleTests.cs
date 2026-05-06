using System.Net;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreMvcTemplate.Web.Tests.Integration;

// Krajno-kraj testovi za feature toggles. Unit testovite proveruvaat deka
// FeatureGateAttribute vraka 404 koga manager-ot vraka false. Ovde proveruvame
// sceloto: confg flag = false -> realna HTTP request -> 404.
//
// Sekoj test boot-uva sopstven factory so razlicen Features:* override za da
// ne se ushtetuvaat eden so drug.
public class FeatureToggleTests
{
    [Fact]
    public async Task Profile_WhenProfileManagementOff_Returns404()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.ConfigurationOverrides["Features:ProfileManagement"] = "false";

        var email = await CreateConfirmedUserAsync(factory);
        var client = factory.CreateClient();
        await SignInAsync(client, email);

        var response = await client.GetAsync("/Profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_WhenAdminAreaOff_Returns404()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.ConfigurationOverrides["Features:AdminArea"] = "false";

        var email = await CreateConfirmedUserAsync(factory, role: "Admin");
        var client = factory.CreateClient();
        await SignInAsync(client, email);

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenRegistrationOff_Returns404()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.ConfigurationOverrides["Features:Registration"] = "false";

        var client = factory.CreateClient();

        var response = await client.GetAsync("/Account/Register");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LoginPage_WhenExternalLoginsOff_HasNoExternalButtons()
    {
        // ExternalLogins = false -> AddExternalAuthentication ne se vika -> nema
        // schemes registrirani -> Login stranata ne render-uva niedna provider button.
        using var factory = new CustomWebApplicationFactory();
        factory.ConfigurationOverrides["Features:ExternalLogins"] = "false";

        var client = factory.CreateClient();
        var response = await client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // "Or sign in with" e tekstot vo Login.cshtml koj se prikazuva samo ako
        // postojat external schemes. Ne treba da go ima.
        Assert.DoesNotContain("Or sign in with", html);
    }

    [Fact]
    public async Task Register_WhenEmailConfirmationOff_SignsInImmediately()
    {
        // Posebniot kombinaciski test: ako EmailConfirmation = false, registracija
        // pravi user so EmailConfirmed = true i go signira odma. Toa e
        // alternativen flow napraven vo feature-toggles branch.
        using var factory = new CustomWebApplicationFactory();
        factory.ConfigurationOverrides["Features:EmailConfirmation"] = "false";

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var email = $"noconfirm-{Guid.NewGuid():N}@test.local";

        var response = await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Register",
            new[]
            {
                new KeyValuePair<string, string>("Name", "Test"),
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1"),
                new KeyValuePair<string, string>("ConfirmPassword", "Password1")
            });

        // Po uspeshna registracija so EmailConfirmation off, redirect kon "/" so
        // signed-in cookie. Bez confirmation email step.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        // I nikako ne se prati confirmation email
        Assert.Empty(factory.EmailSender.SentMessages);

        // I user-ot vo bazata e EmailConfirmed = true
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user!.EmailConfirmed);
    }

    // ---------- Helpers (kopija od AuthorizationTests - moze za posle da se
    // izvajat vo TestUserHelper, ama za sega e OK ovde za clarity) ----------

    private static async Task<string> CreateConfirmedUserAsync(CustomWebApplicationFactory factory, string? role = null)
    {
        var email = $"toggle-{Guid.NewGuid():N}@test.local";
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Test",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "Password1");

        if (role is not null)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
            await userManager.AddToRoleAsync(user, role);
        }

        return email;
    }

    private static async Task SignInAsync(HttpClient client, string email)
    {
        await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Login",
            new[]
            {
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1")
            });
    }
}
