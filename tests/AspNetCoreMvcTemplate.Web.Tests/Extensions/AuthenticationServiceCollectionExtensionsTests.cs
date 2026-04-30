using AspNetCoreMvcTemplate.Web.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreMvcTemplate.Web.Tests.Extensions;

public class AuthenticationServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddExternalAuthentication_DoesNotRegisterProviders_WhenClientIdsAreEmpty()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "",
                ["Authentication:Microsoft:ClientId"] = ""
            })
            .Build();

        services.AddExternalAuthentication(configuration);
        var provider = services.BuildServiceProvider();

        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await schemeProvider.GetAllSchemesAsync();

        Assert.DoesNotContain(schemes, s => s.Name == "Google");
        Assert.DoesNotContain(schemes, s => s.Name == "Microsoft");
    }

    [Fact]
    public async Task AddExternalAuthentication_DoesNotRegisterProvider_WhenClientSecretIsMissing()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // ClientId e postaven ama ClientSecret e prazen - provajderot ke crashne
        // pri token exchange, zatoa voopshto ne go registrirame.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "google-test-id",
                ["Authentication:Google:ClientSecret"] = "",
                ["Authentication:Microsoft:ClientId"] = "microsoft-test-id"
            })
            .Build();

        services.AddExternalAuthentication(configuration);
        var provider = services.BuildServiceProvider();

        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await schemeProvider.GetAllSchemesAsync();

        Assert.DoesNotContain(schemes, s => s.Name == "Google");
        Assert.DoesNotContain(schemes, s => s.Name == "Microsoft");
    }

    [Fact]
    public async Task AddExternalAuthentication_RegistersProviders_WhenClientIdsAreConfigured()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "google-test-id",
                ["Authentication:Google:ClientSecret"] = "google-test-secret",
                ["Authentication:Microsoft:ClientId"] = "microsoft-test-id",
                ["Authentication:Microsoft:ClientSecret"] = "microsoft-test-secret"
            })
            .Build();

        services.AddExternalAuthentication(configuration);
        var provider = services.BuildServiceProvider();

        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await schemeProvider.GetAllSchemesAsync();

        Assert.Contains(schemes, s => s.Name == "Google");
        Assert.Contains(schemes, s => s.Name == "Microsoft");
    }
}
