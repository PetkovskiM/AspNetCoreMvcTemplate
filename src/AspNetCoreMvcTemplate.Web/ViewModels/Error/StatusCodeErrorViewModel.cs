namespace AspNetCoreMvcTemplate.Web.ViewModels.Error;

public class StatusCodeErrorViewModel
{
    public int StatusCode { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? OriginalPath { get; init; }

    public string? OriginalQueryString { get; init; }
}
