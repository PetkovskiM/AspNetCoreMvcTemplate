namespace AspNetCoreMvcTemplate.Web.Features
{
    // Apstrakcija za proveruvanje na feature toggles.
    // Koristi se vo controllers, views, action filters - sekade kade ni treba
    // run-time odluka "dali ovaa feature e vklucena?".
    public interface IFeatureManager
    {
        bool IsEnabled(string featureName);
    }
}
