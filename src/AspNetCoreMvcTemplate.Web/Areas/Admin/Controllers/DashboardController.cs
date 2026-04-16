using AspNetCoreMvcTemplate.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreMvcTemplate.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
