using AspNetCoreMvcTemplate.Web.Features;
using Microsoft.Extensions.Options;

namespace AspNetCoreMvcTemplate.Web.Tests.Features;

public class FeatureManagerTests
{
    [Fact]
    public void IsEnabled_ReturnsTrue_WhenFeatureFlagIsTrue()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new FeatureOptions
        {
            AdminArea = true
        });
        var manager = new FeatureManager(options);

        Assert.True(manager.IsEnabled(nameof(FeatureOptions.AdminArea)));
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenFeatureFlagIsFalse()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new FeatureOptions
        {
            ProfileManagement = false
        });
        var manager = new FeatureManager(options);

        Assert.False(manager.IsEnabled(nameof(FeatureOptions.ProfileManagement)));
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenFeatureNameDoesNotExist()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new FeatureOptions());
        var manager = new FeatureManager(options);

        // Nepoznata feature == isklucena. Bezbedna default vrednost.
        Assert.False(manager.IsEnabled("NonExistentFeature"));
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenFeatureNameIsEmpty()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new FeatureOptions());
        var manager = new FeatureManager(options);

        Assert.False(manager.IsEnabled(string.Empty));
    }
}
