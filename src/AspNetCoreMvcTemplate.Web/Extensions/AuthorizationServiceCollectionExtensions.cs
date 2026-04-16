using AspNetCoreMvcTemplate.Web.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreMvcTemplate.Web.Extensions
{
    public static class AuthorizationServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                // Ednostavna role based policy. Zamena za [Authorize(Roles = "Admin")].
                options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                    policy.RequireRole(Roles.Admin));

                // Custom requirement - logikata e vo ActiveUserAuthorizationHandler.
                options.AddPolicy(AuthorizationPolicies.ActiveUser, policy =>
                    policy.Requirements.Add(new ActiveUserRequirement()));
            });

            return services;
        }
    }
}
