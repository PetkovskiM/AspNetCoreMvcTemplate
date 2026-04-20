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

                // Claims based policy. email_verified claim se zima od ApplicationUserClaimsPrincipalFactory.
                options.AddPolicy(AuthorizationPolicies.RequireEmailConfirmed, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx =>
                              ctx.User.HasClaim(c => c.Type == "email_verified" && c.Value == "true")));

                // Custom requirement - logikata e vo ActiveUserAuthorizationHandler.
                options.AddPolicy(AuthorizationPolicies.ActiveUser, policy =>
                    policy.Requirements.Add(new ActiveUserRequirement()));
            });

            return services;
        }
    }
}
