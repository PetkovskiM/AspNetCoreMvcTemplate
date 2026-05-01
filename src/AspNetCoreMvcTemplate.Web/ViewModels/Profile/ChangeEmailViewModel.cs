using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.ViewModels.Profile
{
    public class ChangeEmailViewModel
    {
        [Display(Name = "Current email")]
        public string CurrentEmail { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "New email")]
        public string NewEmail { get; set; } = string.Empty;
    }
}
