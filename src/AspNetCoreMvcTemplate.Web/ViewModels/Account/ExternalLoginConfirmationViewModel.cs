using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.ViewModels.Account
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }

        // Ime na provajderot, samo za prikaz vo view-ot.
        public string? ProviderDisplayName { get; set; }
    }
}
