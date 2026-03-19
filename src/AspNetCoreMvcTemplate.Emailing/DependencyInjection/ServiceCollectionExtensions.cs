using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Emailing.Options;
using AspNetCoreMvcTemplate.Emailing.Providers.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreMvcTemplate.Emailing.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmailing(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EmailingOptions>(configuration.GetSection(EmailingOptions.SectionName));

            var emailingOptions = configuration
                .GetSection(EmailingOptions.SectionName)
                .Get<EmailingOptions>() ?? new EmailingOptions();

            if (string.Equals(emailingOptions.Provider, "Logging", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEmailSender, LoggingEmailSender>();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported email provider '{emailingOptions.Provider}'.");
            }

            return services;
        }
    }
}
