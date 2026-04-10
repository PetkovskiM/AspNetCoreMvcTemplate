namespace AspNetCoreMvcTemplate.Web.Areas.Admin.ViewModels
{
    public class DeleteRoleViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int UserCount { get; set; }

        public bool IsProtected { get; set; }
    }
}
