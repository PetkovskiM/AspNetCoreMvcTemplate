using AspNetCoreMvcTemplate.Web.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AspNetCoreMvcTemplate.Web.Tests.Features;

public class FeatureGateAttributeTests
{
    [Fact]
    public void OnAuthorization_Returns404_WhenFeatureIsDisabled()
    {
        var featureManager = new Mock<IFeatureManager>();
        featureManager.Setup(x => x.IsEnabled("AdminArea")).Returns(false);

        var context = CreateContext(featureManager.Object);
        var filter = new FeatureGateAttribute("AdminArea");

        filter.OnAuthorization(context);

        Assert.IsType<NotFoundResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_AllowsRequest_WhenFeatureIsEnabled()
    {
        var featureManager = new Mock<IFeatureManager>();
        featureManager.Setup(x => x.IsEnabled("AdminArea")).Returns(true);

        var context = CreateContext(featureManager.Object);
        var filter = new FeatureGateAttribute("AdminArea");

        filter.OnAuthorization(context);

        // context.Result ostanuva null koga filter-ot ne short-circuit-uva.
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_Throws_WhenFeatureManagerIsNotRegistered()
    {
        // Misskonfiguracija (zaboraven AddFeatureManagement() call) treba glasno
        // da se manifestira preku exception, a ne da se prikrie kako 404.
        // 404 bi izgledalo identicno so "feature isklucena" - loso za debug.
        var context = CreateContext(featureManager: null);
        var filter = new FeatureGateAttribute("AdminArea");

        Assert.Throws<InvalidOperationException>(() => filter.OnAuthorization(context));
    }

    private static AuthorizationFilterContext CreateContext(IFeatureManager? featureManager)
    {
        var services = new ServiceCollection();
        if (featureManager is not null)
        {
            services.AddSingleton(featureManager);
        }

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
