namespace AspNetCoreMvcTemplate.Web.ViewModels.Error;

public class ServerErrorViewModel
{
    public string? RequestId { get; init; }

    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);

    public string? OriginalPath { get; init; }
}
