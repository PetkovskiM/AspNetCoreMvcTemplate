using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.ViewModels.Profile
{
    public class ChangeNameViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;
    }
}
