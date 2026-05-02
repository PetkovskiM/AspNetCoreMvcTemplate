using System.Reflection;
using Microsoft.Extensions.Options;

namespace AspNetCoreMvcTemplate.Web.Features
{
    // Trivijalna implementacija - chita FeatureOptions preku IOptions i vraka
    // bool vrednosta na imenuvaniot property preku reflection. Reflection-ot
    // e brz (~mikrosekundi) i propertite se vsushnost samo 5 - ne e bottleneck.
    //
    // Ako se prefrlime na po-slozhena strategija (na primer, feature flags vo
    // baza, percentage rollout, A/B testing), ja menjame samo ovaa klasa - site
    // konzumeri (controllers, views, filters) baraat IFeatureManager interfejsot
    // i ne se zasegnati od promena.
    public class FeatureManager : IFeatureManager
    {
        private readonly FeatureOptions options;

        public FeatureManager(IOptions<FeatureOptions> options)
        {
            this.options = options.Value;
        }


        // moze i vaka Dictionary<string, Func<FeatureOptions, bool>> ama i nemora 
        public bool IsEnabled(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                return false;
            }

            // IgnoreCase za da ne e fragile na typo-i vo casing.
            // case-insensitive e pobezbedna default vrednost.
            var property = typeof(FeatureOptions).GetProperty(
                featureName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property is null || property.PropertyType != typeof(bool))
            {
                return false;
            }

            return (bool)(property.GetValue(options) ?? false);
        }
    }
}
