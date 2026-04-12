using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.Areas.Admin.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required]
        [StringLength(64, MinimumLength = 2, ErrorMessage = "The {0} must be between {2} and {1} characters.")]
        [Display(Name = "Role name")]
        public string Name { get; set; } = string.Empty;
    }
}
