using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCoreMvcTemplate.Web.Features
{
    // Action filter koj vrakja 404 ako feature-ot e isklucen, ili dozvoluva
    // request-ot da prodolzhi normalno ako e vklucen.
    //
    // Zoshto 404 a ne 403? Iz perspektivata na korisnikot, isklucena feature
    // ne postoi vo aplikacijata - 404 (Not Found) e iskreniot odgovor.
    // 403 (Forbidden) bi sugeriralo "postoi ama nemash dozvola" - shto leak-uva
    // informacija za feature-ite shto gi imame.
    //
    // Koristenje:
    //   [FeatureGate(nameof(FeatureOptions.AdminArea))]
    //   public class DashboardController : Controller { ... }
    //
    // IAuthorizationFilter se izvrshuva NAJRANO vo pipeline-ot - pred model
    // binding, pred action filters. Tochno kade sakame - ne sakame da rasipuvame
    // resursi binduvajki model za action koja po toa ke se otkaze.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class FeatureGateAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string featureName;

        public FeatureGateAttribute(string featureName)
        {
            this.featureName = featureName;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Resolvirame IFeatureManager od request-scoped DI kontejnerot.
            // Atributite ne mozat da imaat constructor injection - ova e standarden
            // workaround vo MVC.
            var featureManager = context.HttpContext.RequestServices
                .GetService(typeof(IFeatureManager)) as IFeatureManager;

            if (featureManager is null || !featureManager.IsEnabled(featureName))
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
