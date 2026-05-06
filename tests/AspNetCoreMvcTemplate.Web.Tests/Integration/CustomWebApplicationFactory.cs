using AspNetCoreMvcTemplate.Emailing.Abstractions;
using AspNetCoreMvcTemplate.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AspNetCoreMvcTemplate.Web.Tests.Integration;

// Boots the full Program.cs pipeline in-memory, with these test-specific overrides:
//
//   1. SQL Server -> SQLite in-memory (Mode=Memory;Cache=Shared so multiple
//      DbContext instances ja gledaat istata baza vo ramki na eden test fixture)
//   2. Real IEmailSender -> TestEmailSender (capture, ne send)
//   3. File-system DataProtection -> EphemeralDataProtectionProvider
//      (test-friendly, ne zapishuva keys na disk)
//   4. FeatureOptions overrides preku in-memory configuration -
//      sekoj test fixture moze da boot-ne so razlicni feature flags
//
// IClassFixture<CustomWebApplicationFactory> spodeluva eden factory megu site
// testovi vo edna klasa (brz startup). Sekoja klasa kaja saka razlicni feature
// flags ima svoja sopstvena podklasa.
// konfigurariraj za when you boot my app for tests, use these hooks during startup.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Connection ostanuva otvorena za celiot zhivot na factory-to.
    // Koga toa kje se zatvori, SQLite in-memory bazata isceznuva.
    private readonly SqliteConnection connection;

    // Public za da test fixture-ite gi setuvaat OVERRIDES PRED CreateClient.
    public Dictionary<string, string?> ConfigurationOverrides { get; } = new();

    public TestEmailSender EmailSender { get; } = new();

    public CustomWebApplicationFactory()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Overrides za config keys (na primer, Features:AdminArea = false).
            // Mora po site drugi providers za da imaat priority.
            config.AddInMemoryCollection(ConfigurationOverrides!);
        });

        builder.ConfigureServices(services =>
        {
            // Otstrani go SQL Server DbContext (registriran vo Program.cs)
            ReplaceDbContext(services);

            // Otstrani go realniot IEmailSender (LoggingEmailSender) i registriraj
            // Test version. AddSingleton so explicit instance za da gi citame
            //  porakite preku factory.EmailSender.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);

            // DataProtection vo testovi - ephemeral keys, bez file system pristap.
            services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();
        });
    }

    private void ReplaceDbContext(IServiceCollection services)
    {
        // AddDbContext vo Program.cs registrira ne samo DbContextOptions<T> tuku
        // i lambdi vo IConfigureOptions<DbContextOptions<T>> koi sodrzhat UseSqlServer.
        // Ako ne gi otstranime SITE EF SQL Server descriptors, novata UseSqlite-options
        // ke se kombinira so postojnata UseSqlServer-options i ke puka:
        // "Multiple relational database provider configurations found".
        var efDescriptors = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>) ||
                (d.ServiceType.FullName?.Contains("EntityFrameworkCore.SqlServer", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.ImplementationType?.FullName?.Contains("EntityFrameworkCore.SqlServer", StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        foreach (var d in efDescriptors)
        {
            services.Remove(d);
        }

        // Sega ima samo SQLite provider services - bez konflikt.
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(connection);
        });
    }

    // Posle build na celiot host, otvori scope i EnsureCreated. Ova ne moze
    // da se napravi vo ConfigureServices provider-ot tamu e
    // statichen i Database.EnsureCreated bara komplet konfiguriran provider.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            connection.Dispose();
        }
        base.Dispose(disposing);
    }
}

