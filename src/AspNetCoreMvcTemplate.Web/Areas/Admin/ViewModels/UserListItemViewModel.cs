namespace AspNetCoreMvcTemplate.Web.Areas.Admin.ViewModels
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Roles { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public bool IsProtected { get; set; }
    }
}
