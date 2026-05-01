using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AspNetCoreMvcTemplate.Web.ViewModels.Profile
{
    public class ProfileIndexViewModel
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public bool HasPassword { get; set; }

        // Provajderi koi se vekje povrzani so user-ot.
        public IList<UserLoginInfo> CurrentLogins { get; set; } = new List<UserLoginInfo>();

        // Provajderi koi se konfigurirani vo aplikacijata no ushte ne se povrzani.
        public IList<AuthenticationScheme> AvailableProviders { get; set; } = new List<AuthenticationScheme>();

        // Statusna poraka po redirect (TempData)
        public string? StatusMessage { get; set; }
    }
}
