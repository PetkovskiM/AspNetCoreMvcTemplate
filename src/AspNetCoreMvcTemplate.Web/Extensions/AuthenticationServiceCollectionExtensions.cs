using Microsoft.AspNetCore.Authentication;

namespace AspNetCoreMvcTemplate.Web.Extensions
{
    // Registrira eksterni login provajderi (Google, Microsoft, Facebook) samo ako se
    // postaveni i ClientId i ClientSecret (resp. AppId / AppSecret za Facebook) vo
    // konfiguracijata. Na razvoj idat od user-secrets, na staging/prod od environment
    // variable injektirani od CD pipeline vo web.config.
    //
    // Ako keys ne se postaveni - provajderot ne se registrira, butonot na Login stranata
    // ne se prikazuva, aplikacijata ne crash-nuva. Istata logika raboti niz site okruzhuvanja.
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var authBuilder = services.AddAuthentication();

            // Bara i ClientId i ClientSecret. Ako samo eden e prisuten, provajderot
            // ke crashne pri token exchange - po-dobro e voopsho da ne go registrirame.
            var googleClientId = configuration["Authentication:Google:ClientId"];
            var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId)
                && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    // Google vrakja email_verified claim direktno od id_tokenot.
                    // Avtomatski se mapira na ClaimsPrincipal - nema dopolnitelna konfiguracija.
                    options.Events.OnRemoteFailure = HandleRemoteFailure;
                });
            }

            var microsoftClientId = configuration["Authentication:Microsoft:ClientId"];
            var microsoftClientSecret = configuration["Authentication:Microsoft:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(microsoftClientId)
                && !string.IsNullOrWhiteSpace(microsoftClientSecret))
            {
                authBuilder.AddMicrosoftAccount(options =>
                {
                    options.ClientId = microsoftClientId;
                    options.ClientSecret = microsoftClientSecret;
                    options.Events.OnRemoteFailure = HandleRemoteFailure;
                });
            }

            // Facebook koristi AppId/AppSecret terminologija (kako vo Meta for Developers
            // konzolata) - po internoto se mapira na ClientId/ClientSecret. Facebook
            // garantira deka site emails se verificirani, taka shto novite useri se
            // kreiraat so EmailConfirmed = true (isto kako Google).
            var facebookAppId = configuration["Authentication:Facebook:AppId"];
            var facebookAppSecret = configuration["Authentication:Facebook:AppSecret"];
            if (!string.IsNullOrWhiteSpace(facebookAppId)
                && !string.IsNullOrWhiteSpace(facebookAppSecret))
            {
                authBuilder.AddFacebook(options =>
                {
                    options.AppId = facebookAppId;
                    options.AppSecret = facebookAppSecret;
                    options.Events.OnRemoteFailure = HandleRemoteFailure;
                });
            }

            return services;
        }

        // Bez ovaa hooka, OAuth handler-ot frla exception koga userot kje klikne
        // "Cancel" na consent stranata na provajderot (Facebook posebno) - ke se
        // pokaze 500 namesto friendly poraka. Ovde gi presretvame i go redirektirame
        // browser-ot nazad do originalniot RedirectUri (sign-in callback ili link callback)
        // so remoteError query param - controllerite vekje znaat kako da go handlat.
        private static Task HandleRemoteFailure(RemoteFailureContext context)
        {
            var redirectUrl = context.Properties?.RedirectUri ?? "/Account/Login";
            var separator = redirectUrl.Contains('?') ? "&" : "?";
            var error = Uri.EscapeDataString(
                context.Failure?.Message ?? "External sign-in was cancelled or failed.");

            context.Response.Redirect($"{redirectUrl}{separator}remoteError={error}");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    }
}
