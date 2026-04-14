using Serilog;

namespace AspNetCoreMvcTemplate.Web.Extensions
{
    public static class SerilogServiceCollectionExtensions
    {
        public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
            });

            return builder;
        }
    }
}
