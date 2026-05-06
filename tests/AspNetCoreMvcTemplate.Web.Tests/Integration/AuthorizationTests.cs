using System.Net;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreMvcTemplate.Web.Tests.Integration;

// Krajno-kraj testovi za policy-based authorization. Proveruvaat deka:
//   - AdminOnly policy gi blokira ne-admin korisnicite (403/redirect)
//   - AdminOnly policy gi propusta admin-ite
//   - RequireConfirmedEmail vo cookie configot blokira unconfirmed useri
//
// Za razlika od unit testovite (koi mockuvaat policies), ovde sve odi preku
// realniot DI + middleware pipeline.
public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AdminArea_WithoutAdminRole_ReturnsForbiddenOrRedirect()
    {
        // Authenticated user bez Admin role - AdminOnly policy go blokira.
        // ASP.NET Core po default vraka 302 redirect kon AccessDenied za
        // authenticated-ama-not-authorized slucai (a ne 403).
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var email = await CreateConfirmedUserAsync();
        await SignInAsync(client, email, "Password1");

        var response = await client.GetAsync("/Admin");

        // Cookie auth confugurira AccessDeniedPath = "/Account/AccessDenied" -
        // expectani 302 redirect.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("AccessDenied", response.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task AdminArea_WithAdminRole_ReturnsOk()
    {
        var client = factory.CreateClient();
        var email = await CreateConfirmedUserAsync(role: "Admin");
        await SignInAsync(client, email, "Password1");

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnconfirmedUser_CannotSignIn()
    {
        // Slica zashto: nashata RequireConfirmedEmail policy + Identity options
        // RequireConfirmedEmail = true (od Program.cs) blokiraat sign-in.
        // SignInManager vrakja IsNotAllowed - kontrolerot ne postavuva auth
        // cookie. Posle "login" - userot e neavtenticiran.
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var email = await CreateUnconfirmedUserAsync();

        var loginResponse = await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Login",
            new[]
            {
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password1")
            });

        // Login formata se vrakja so error message - login ne uspeal.
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Probaj zatvoreni resursi - userot ne treba da bide avtenticiran.
        var profileResponse = await client.GetAsync("/Profile");
        Assert.Equal(HttpStatusCode.Redirect, profileResponse.StatusCode);
    }

    // ---------- Helpers ----------

    private async Task<string> CreateConfirmedUserAsync(string? role = null)
    {
        var email = $"auth-{Guid.NewGuid():N}@test.local";
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Test User",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, "Password1");
        Assert.True(createResult.Succeeded);

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

    private async Task<string> CreateUnconfirmedUserAsync()
    {
        var email = $"unconfirmed-{Guid.NewGuid():N}@test.local";
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Test User",
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, "Password1");
        Assert.True(result.Succeeded);
        return email;
    }

    private static async Task SignInAsync(HttpClient client, string email, string password)
    {
        var response = await AntiforgeryHelper.PostWithAntiforgeryAsync(
            client,
            "/Account/Login",
            new[]
            {
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", password)
            });

        // Po uspeshen login, response e 302 ili (so AllowAutoRedirect=true) 200
        // posle redirect. Vo dvata slucai user-ot e signed-in.
        if (response.StatusCode != HttpStatusCode.Redirect && !response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Sign-in failed with status {response.StatusCode}.");
        }
    }
}
