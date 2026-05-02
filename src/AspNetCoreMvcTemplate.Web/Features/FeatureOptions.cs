namespace AspNetCoreMvcTemplate.Web.Features
{
    // Strongly-typed mapping na "Features" sekcijata vo appsettings.json.
    // Default vrednosti se site `true` - ako nekoj klonira template-ot i ne
    // dodade Features sekcija, app-ot raboti kako i predtoa (sve vkluceno).
    public class FeatureOptions
    {
        // Admin Area (Dashboard, Roles CRUD, Users management).
        public bool AdminArea { get; set; } = true;

        // Self-service profile (change name/password/email, manage linked logins).
        public bool ProfileManagement { get; set; } = true;

        // Public user registration (Register stranata + RegisterConfirmation).
        // Off = samo admin moze da kreira useri preku Admin area.
        public bool Registration { get; set; } = true;

        // Hard switch za eksterni provajderi. Off = AddExternalAuthentication
        // se preskoknuva celosno bez ogled na ClientId konfiguracijata.
        public bool ExternalLogins { get; set; } = true;

        // Off = users get EmailConfirmed = true on register, no confirmation email,
        // RequireConfirmedEmail = false (signing in works without email confirmation).
        public bool EmailConfirmation { get; set; } = true;
    }
}
