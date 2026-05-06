using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace AspNetCoreMvcTemplate.Web.Tests.Integration;

// Vraka antiforgery (CSRF) tokenot od HTML formata na zadadeniot URL i
// gi spojuva so dadenite form fields da se napravi POST.
//
// Razionala: kontrolerite koristat [AutoValidateAntiforgeryToken], shto znachi
// site POST-i baraat validen __RequestVerificationToken (i vo cookie i vo form
// body). Vo realen browser flow, GET-ot na formata ja postavuva cookie-to i
// hidden field-ot. Vo testovi go simulirame istoto: GET, parse, POST.
//
// Pakraj sve, ne sakame da go isklucime CSRF vo testovi - cel e da ja testirame
// realnata pipeline, vkluchitelno antiforgery.
public static class AntiforgeryHelper
{
    private static readonly Regex TokenRegex = new(
        """<input[^>]*name="__RequestVerificationToken"[^>]*value="([^"]+)"[^>]*/?>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // GET-uva URL, parse-uva tokenot od formata, POST-uva so istiot client (taka
    // antiforgery cookie-to e zachuvana).
    //
    // tokenSourceUrl: po default e istiot kako postUrl (tipichniot slucaj - GET
    // pa POST na ista forma). Za actions koi nemaat GET (kako Logout), prosledi
    // razlichen URL kade tokenot moze da se najde (na primer "/").
    public static async Task<HttpResponseMessage> PostWithAntiforgeryAsync(
        HttpClient client,
        string url,
        IEnumerable<KeyValuePair<string, string>> formFields,
        string? tokenSourceUrl = null)
    {
        var sourceUrl = tokenSourceUrl ?? url;
        var getResponse = await client.GetAsync(sourceUrl);
        getResponse.EnsureSuccessStatusCode();
        var html = await getResponse.Content.ReadAsStringAsync();

        var match = TokenRegex.Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Could not find antiforgery token on GET {sourceUrl}. Response was {html.Length} chars.");
        }

        var token = match.Groups[1].Value;

        var fields = formFields.ToList();
        fields.Add(new KeyValuePair<string, string>("__RequestVerificationToken", token));

        var content = new FormUrlEncodedContent(fields);
        return await client.PostAsync(url, content);
    }
}
