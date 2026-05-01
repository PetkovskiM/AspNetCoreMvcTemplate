using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.ViewModels.Profile
{
    // Koristi se za useri koi nemaat lokalna lozinka (registriraha preku eksteren provajder).
    public class SetPasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
