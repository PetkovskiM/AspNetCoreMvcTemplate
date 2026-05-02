using AspNetCoreMvcTemplate.Web.Authorization;
using AspNetCoreMvcTemplate.Web.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreMvcTemplate.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [FeatureGate(nameof(FeatureOptions.AdminArea))]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
