using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Web.ViewModels.Account
{
    public class EmailInputViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
