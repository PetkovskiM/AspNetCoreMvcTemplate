using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AspNetCoreMvcTemplate.Web.Authorization
{
    // Demonstrira kako da se injektira UserManager vo handler i da se kombiniraat
    // proverki na ClaimsPrincipal so podatoci od user store-ot.
    public class ActiveUserAuthorizationHandler : AuthorizationHandler<ActiveUserRequirement>
    {
        private readonly UserManager<ApplicationUser> userManager;

        public ActiveUserAuthorizationHandler(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ActiveUserRequirement requirement)
        {
            if (context.User?.Identity is null || !context.User.Identity.IsAuthenticated)
            {
                return;
            }

            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return;
            }

            if (!user.EmailConfirmed)
            {
                return;
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return;
            }

            context.Succeed(requirement);
        }
    }
}
