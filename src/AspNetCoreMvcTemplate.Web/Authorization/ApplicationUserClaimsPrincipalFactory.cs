using System.Security.Claims;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AspNetCoreMvcTemplate.Web.Authorization
{
    // Claims dodadeni tuka se cuvaat vo auth cookie i se koristat za authorization checks.
    // Be careful: Promenite vo DB nema da afektiraat dodeka userot ne napravi re-authenticate.

    public class ApplicationUserClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions > optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            identity.AddClaim(new Claim("email_verified", user.EmailConfirmed ? "true" : "false"));

            if (!string.IsNullOrWhiteSpace(user.Name))
            {
                identity.AddClaim(new Claim("name", user.Name));
            }

            return identity;
        }
    }
}
