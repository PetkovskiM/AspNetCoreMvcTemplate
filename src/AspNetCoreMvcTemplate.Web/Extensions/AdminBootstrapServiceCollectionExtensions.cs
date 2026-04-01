using System.ComponentModel.DataAnnotations;
using AspNetCoreMvcTemplate.Web.Options;
using AspNetCoreMvcTemplate.Web.Services;

namespace AspNetCoreMvcTemplate.Web.Extensions
{
    public static class AdminBootstrapServiceCollectionExtensions
    {
        public static IServiceCollection AddAdminBootstrap(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<AdminBootstrapOptions>()
                .Bind(configuration.GetSection(AdminBootstrapOptions.SectionName))
                .Validate(
                    options => !options.Enabled ||
                               (!string.IsNullOrWhiteSpace(options.RoleName) &&
                                !string.IsNullOrWhiteSpace(options.Email) &&
                                !string.IsNullOrWhiteSpace(options.Password) &&
                                !string.IsNullOrWhiteSpace(options.DisplayName)),
                    "Admin bootstrap settings must include role name, email, password, and display name when enabled.")
                .Validate(
                    options => !options.Enabled || new EmailAddressAttribute().IsValid(options.Email),
                    "Admin bootstrap email must be a valid email address when enabled.");

            services.AddScoped<AdminBootstrapSeeder>();

            return services;
        }
    }
}
