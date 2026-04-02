using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.Options
{
    public class AdminBootstrapOptions
    {
        public const string SectionName = "AdminBootstrap";

        public bool Enabled { get; set; }

        public string RoleName { get; set; } = "Admin";

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;
    }
}
