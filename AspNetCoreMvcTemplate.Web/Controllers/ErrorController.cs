using AspNetCoreMvcTemplate.Web.ViewModels.Error;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AspNetCoreMvcTemplate.Web.Controllers;

[Route("Error")]
public class ErrorController : Controller
{

    [HttpGet("")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult ServerError()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        var model = new ServerErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            OriginalPath = exceptionFeature?.Path
        };

        return View(model);
    }


    [HttpGet("{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodeError(int statusCode)
    {
        // Implementacijata na IStatusCodeReExecuteFeature e dodadena od strana na ASP.NET Core middleware
        var statusCodeFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        var model = statusCode switch
        {
            404 => new StatusCodeErrorViewModel
            {
                StatusCode = 404,
                Title = "Page Not Found",
                Message = "Sorry, the resource you requested could not be found.",
                OriginalPath = statusCodeFeature?.OriginalPath,
                OriginalQueryString = statusCodeFeature?.OriginalQueryString
            },
            403 => new StatusCodeErrorViewModel
            {
                StatusCode = 403,
                Title = "Access Denied",
                Message = "You do not have permission to access this resource.",
                OriginalPath = statusCodeFeature?.OriginalPath,
                OriginalQueryString = statusCodeFeature?.OriginalQueryString
            },
            _ => new StatusCodeErrorViewModel
            {
                StatusCode = statusCode,
                Title = "Request Error",
                Message = "The request could not be completed.",
                OriginalPath = statusCodeFeature?.OriginalPath,
                OriginalQueryString = statusCodeFeature?.OriginalQueryString
            }
        };

        Response.StatusCode = statusCode;

        return View(model);
    }

}
