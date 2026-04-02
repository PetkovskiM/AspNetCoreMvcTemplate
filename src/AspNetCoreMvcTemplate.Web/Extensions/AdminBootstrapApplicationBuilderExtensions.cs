using AspNetCoreMvcTemplate.Web.Services;

namespace AspNetCoreMvcTemplate.Web.Extensions
{
    public static class AdminBootstrapApplicationBuilderExtensions
    {
        public static async Task SeedAdminBootstrapAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();

            var seeder = scope.ServiceProvider.GetRequiredService<AdminBootstrapSeeder>();
            await seeder.SeedAsync();
        }
    }
}
