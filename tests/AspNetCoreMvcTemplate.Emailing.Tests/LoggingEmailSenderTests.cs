using AspNetCoreMvcTemplate.Emailing.Models;
using AspNetCoreMvcTemplate.Emailing.Options;
using AspNetCoreMvcTemplate.Emailing.Providers.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AspNetCoreMvcTemplate.Emailing.Tests;

public class LoggingEmailSenderTests
{
    [Fact]
    public async Task SendAsync_ReturnsFailure_WhenEmailingIsDisabled()
    {
        // Arrange
        var logger = NullLogger<LoggingEmailSender>.Instance;

        var options = Microsoft.Extensions.Options.Options.Create(new EmailingOptions
        {
            Enabled = false,
            Provider = "Logging",
            FromEmail = "noreply@test.local",
            FromName = "Test App"
        });

        var sender = new LoggingEmailSender(logger, options);

        var message = new EmailMessage
        {
            To = "user@test.com",
            Subject = "Test Subject",
            HtmlBody = "<p>Hello</p>"
        };

        // Act
        var result = await sender.SendAsync(message);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Email sending is disabled by configuration.", result.ErrorMessage);
    }

    [Fact]
    public async Task SendAsync_ReturnsSuccess_WhenEmailingIsEnabled()
    {
        // Arrange
        var logger = NullLogger<LoggingEmailSender>.Instance;

        var options = Microsoft.Extensions.Options.Options.Create(new EmailingOptions
        {
            Enabled = true,
            Provider = "Logging",
            FromEmail = "noreply@test.local",
            FromName = "Test App"
        });

        var sender = new LoggingEmailSender(logger, options);

        var message = new EmailMessage
        {
            To = "user@test.com",
            Subject = "Test Subject",
            HtmlBody = "<p>Hello</p>"
        };

        // Act
        var result = await sender.SendAsync(message);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
    }
}