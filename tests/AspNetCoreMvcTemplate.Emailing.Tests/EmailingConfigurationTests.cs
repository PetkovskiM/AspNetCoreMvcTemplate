using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Emailing.DependencyInjection;
using AspNetCoreMvcTemplate.Emailing.Options;
using AspNetCoreMvcTemplate.Emailing.Providers.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCoreMvcTemplate.Emailing.Tests;

public class EmailingConfigurationTests
{
    [Fact]
    public void EmailingOptions_HasExpectedDefaultValues()
    {
        // Arrange
        var options = new EmailingOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal("Logging", options.Provider);
        Assert.Equal(string.Empty, options.FromEmail);
        Assert.Equal(string.Empty, options.FromName);
    }

    [Fact]
    public void AddEmailing_RegistersLoggingEmailSender_WhenProviderIsLogging()
    {
        // Arrange
        var services = new ServiceCollection();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Emailing:Enabled"] = "true",
            ["Emailing:Provider"] = "Logging",
            ["Emailing:FromEmail"] = "noreply@test.local",
            ["Emailing:FromName"] = "Test App"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddLogging();
        services.AddEmailing(configuration);

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var emailSender = serviceProvider.GetRequiredService<IEmailSender>();

        // Assert
        Assert.NotNull(emailSender);
        Assert.IsType<LoggingEmailSender>(emailSender);
    }


    [Fact]
    public void AddEmailing_ThrowsInvalidOperationException_WhenProviderIsUnsupported()
    {
        // Arrange
        var services = new ServiceCollection();

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Emailing:Enabled"] = "true",
            ["Emailing:Provider"] = "SendGrid",
            ["Emailing:FromEmail"] = "noreply@test.local",
            ["Emailing:FromName"] = "Test App"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddEmailing(configuration));

        Assert.Equal("Unsupported email provider 'SendGrid'.", exception.Message);
    }
}