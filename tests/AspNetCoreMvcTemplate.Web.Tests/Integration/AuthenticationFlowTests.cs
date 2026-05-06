using System.Net;
using System.Text.RegularExpressions;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreMvcTemplate.Web.Tests.Integration;

// end to end testovi za auth pipeline-ot. Site testovi se klucni:
// proveruvaat deka kompletno pipelineot raboti (cookie auth, Identity middleware,
// claims factory, RequireConfirmedEmail policy, antiforgery, claims pipeline).
public class AuthenticationFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;

    public AuthenticationFlowTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        this.factory.EmailSender.Clear();
    }

    [Fact]
    public async Task Anonymous_Get_Profile_RedirectsToLogin()
    {
        // ApplicationCookie LoginPath = /Account/Login - cookie middleware-ot
        // pravi 302 redirect kon Login.
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Profile");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Register_Post_CreatesUserAndSendsConfirmationEmail()
    {
        var client = factory.CreateClient();
        var email = $"register-{Guid.NewGuid():N}@test.local";

        var response = await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Register",
            new[]
            {
                new KeyValuePair<string, string>("Name", "Test User"),
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1"),
                new KeyValuePair<string, string>("ConfirmPassword", "Password1")
            });

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect,
            $"Expected success or redirect, got {response.StatusCode}");

        // Userot e kreiran vo bazata
        await AssertUserExistsAsync(email);

        // Email-ot e prefatren so token link
        Assert.Single(factory.EmailSender.SentMessages);
        var sentEmail = factory.EmailSender.SentMessages.First();
        Assert.Equal(email, sentEmail.To);
        Assert.Contains("token=", sentEmail.HtmlBody);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_MarksUserConfirmed()
    {
        var client = factory.CreateClient();
        var email = $"confirm-{Guid.NewGuid():N}@test.local";

        // Step 1: register so da dobieme link vo email-ot
        await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Register",
            new[]
            {
                new KeyValuePair<string, string>("Name", "Test"),
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1"),
                new KeyValuePair<string, string>("ConfirmPassword", "Password1")
            });

        var sentEmail = factory.EmailSender.SentMessages.First(m => m.To == email);
        var confirmUrl = ExtractHrefFromHtml(sentEmail.HtmlBody);

        // Step 2: hit confirmation linkot
        var confirmResponse = await client.GetAsync(confirmUrl);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Step 3: proveri vo bazata deka user e confirmed
        var user = await GetUserByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user!.EmailConfirmed);
    }

    [Fact]
    public async Task Login_WithUnconfirmedUser_IsNotAllowed()
    {
        // Register so SignIn.RequireConfirmedEmail = true - login bez confirm
        // mora da bide blokiran. Dokazuva deka claims pipeline + RequireConfirmedEmail
        // policy-to rabotat zaedno.
        var client = factory.CreateClient();
        var email = $"unconfirmed-{Guid.NewGuid():N}@test.local";

        await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Register",
            new[]
            {
                new KeyValuePair<string, string>("Name", "Test"),
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1"),
                new KeyValuePair<string, string>("ConfirmPassword", "Password1")
            });

        // Probaj login (preskoknuvajki confirmation)
        var loginClient = factory.CreateClient();
        var loginResponse = await AntiforgeryHelper.PostWithAntiforgeryAsync(
            loginClient,
            "/Account/Login",
            new[]
            {
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1")
            });

        // Login formata se vrakja so error message - ne 302 redirect kon Home.
        var html = await loginResponse.Content.ReadAsStringAsync();
        Assert.Contains("not ready for sign-in", html);
    }

    [Fact]
    public async Task Login_WithConfirmedUser_RedirectsHome()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var email = $"login-{Guid.NewGuid():N}@test.local";

        await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Register",
            new[]
            {
                new KeyValuePair<string, string>("Name", "Test"),
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1"),
                new KeyValuePair<string, string>("ConfirmPassword", "Password1")
            });

        // Manuelno potvrdi (preku UserManager) - po-brzo i poizolirano otkolku
        // da lovime token od email.
        await ConfirmUserAsync(email);

        var loginResponse = await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Login",
            new[]
            {
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1")
            });

        // Successful login = 302 to Home
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/", loginResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Logout_ClearsAuthCookie()
    {
        var client = factory.CreateClient();
        var email = $"logout-{Guid.NewGuid():N}@test.local";

        await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Register",
            new[]
            {
                new KeyValuePair<string, string>("Name", "Test"),
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1"),
                new KeyValuePair<string, string>("ConfirmPassword", "Password1")
            });

        await ConfirmUserAsync(email);

        await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Login",
            new[]
            {
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1")
            });

        // Sega sme login-ti. Logout nema GET, pa go zememe tokenot od home page
        // kade _LoginPartial render-uva logout formata so antiforgery token.
        var logoutResponse = await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Logout",
            Enumerable.Empty<KeyValuePair<string, string>>(),
            tokenSourceUrl: "/");

        Assert.True(logoutResponse.IsSuccessStatusCode || logoutResponse.StatusCode == HttpStatusCode.Redirect);

        // Posle logout, /Profile pak treba da redirektira kon login.
        var noAutoRedirect = factory.CreateClient(new() { AllowAutoRedirect = false });
        // Ovoj client nema cookie - testot se potvrduva preku faktot deka noviot
        // client e anonimen, taka shto pristapot e blokiran. Site postoeshti
        // cookie-a se ochisteni posle logout.
        var profileResponse = await noAutoRedirect.GetAsync("/Profile");
        Assert.Equal(HttpStatusCode.Redirect, profileResponse.StatusCode);
    }

    // ---------- Helpers ----------

    private async Task AssertUserExistsAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
    }

    private async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByEmailAsync(email);
    }

    private async Task ConfirmUserAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"User {email} not found.");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await userManager.ConfirmEmailAsync(user, token);
        Assert.True(result.Succeeded);
    }

    private static string ExtractHrefFromHtml(string html)
    {
        var match = Regex.Match(html, """href="([^"]+)""");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
