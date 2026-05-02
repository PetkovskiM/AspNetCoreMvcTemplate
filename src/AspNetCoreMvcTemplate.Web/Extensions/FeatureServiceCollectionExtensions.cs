using AspNetCoreMvcTemplate.Web.Features;

namespace AspNetCoreMvcTemplate.Web.Extensions
{
    public static class FeatureServiceCollectionExtensions
    {
        // Binds "Features" sekcijata na FeatureOptions i registrira FeatureManager
        // kako Singleton. FeatureOptions ne se menja vo runtime (samo na startup),
        // taka shto Singleton e bezbedno i efikasno.
        public static IServiceCollection AddFeatureManagement(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<FeatureOptions>(configuration.GetSection("Features"));
            services.AddSingleton<IFeatureManager, FeatureManager>();
            return services;
        }
    }
}
