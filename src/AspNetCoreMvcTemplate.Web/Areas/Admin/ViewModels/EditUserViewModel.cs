using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.Areas.Admin.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "The {0} must be between {2} and {1} characters.")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Email confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Lockout enabled")]
        public bool LockoutEnabled { get; set; }

        public bool IsProtected { get; set; }

        public List<RoleAssignmentViewModel> RoleAssignments { get; set; } = new();
    }
}
