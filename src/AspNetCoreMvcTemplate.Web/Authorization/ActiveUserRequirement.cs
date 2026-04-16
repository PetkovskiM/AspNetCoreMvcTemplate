using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreMvcTemplate.Web.Authorization
{
    // Marker requirement - samo tip, bez logika.
    // Logikata e vo ActiveUserAuthorizationHandler.
    public class ActiveUserRequirement : IAuthorizationRequirement
    {
    }
}
