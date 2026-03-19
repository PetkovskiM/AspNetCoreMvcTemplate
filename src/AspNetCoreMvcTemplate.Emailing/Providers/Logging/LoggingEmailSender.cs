using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Emailing.Models;
using AspNetCoreMvcTemplate.Emailing.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreMvcTemplate.Emailing.Providers.Logging
{
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;
        private readonly EmailingOptions _options;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IOptions<EmailingOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogWarning(
                    "Email sending is disabled. Email to {Recipient} with subject {Subject} was not sent.",
                    message.To,
                    message.Subject);

                return Task.FromResult(new EmailSendResult
                {
                    Succeeded = false,
                    ErrorMessage = "Email sending is disabled by configuration."
                });
            }

            _logger.LogInformation("Sending email using logging provider.");
            _logger.LogInformation("From: {FromName} <{FromEmail}>", _options.FromName, _options.FromEmail);
            _logger.LogInformation("To: {Recipient}", message.To);
            _logger.LogInformation("Subject: {Subject}", message.Subject);
            _logger.LogInformation("HtmlBody: {HtmlBody}", message.HtmlBody);

            if (!string.IsNullOrWhiteSpace(message.TextBody))
            {
                _logger.LogInformation("TextBody: {TextBody}", message.TextBody);
            }

            return Task.FromResult(new EmailSendResult
            {
                Succeeded = true
            });
        }
    }
}
