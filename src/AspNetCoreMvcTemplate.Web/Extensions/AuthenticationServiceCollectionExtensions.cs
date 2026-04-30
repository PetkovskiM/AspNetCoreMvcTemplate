namespace AspNetCoreMvcTemplate.Web.Extensions
{
    // Registrira eksterni login provajderi (Google, Microsoft) samo ako e
    // postaven ClientId vo konfiguracijata. Na razvoj ClientId doaga od user-secrets,
    // na staging/prod doaga od environment variable (injektiran od CD pipeline vo web.config).
    //
    // Ako ClientId e prazen - provajderot ne e registriran, butonot na Login stranata
    // ne se prikazuva, aplikacijata ne crash-nuva. Istata ovaa logika raboti niz site okruzhuvanja.
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
                });
            }

            return services;
        }
    }
}
