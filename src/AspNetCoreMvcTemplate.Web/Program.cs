using AspNetCoreMvcTemplate.Emailing.DependencyInjection;
using AspNetCoreMvcTemplate.Web.Authorization;
using AspNetCoreMvcTemplate.Web.Data;
using AspNetCoreMvcTemplate.Web.Extensions;
using AspNetCoreMvcTemplate.Web.Features;
using AspNetCoreMvcTemplate.Web.Models.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AspNetCoreMvcTemplate.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.AddSerilogLogging();
            builder.Services.AddControllersWithViews();
            builder.Services.AddEmailing(builder.Configuration);
            builder.Services.AddAdminBootstrap(builder.Configuration);
            builder.Services.AddFeatureManagement(builder.Configuration);
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();

            // Chitame Features sekcijata sinhrono za startup-time odluki (Identity options,
            // dali da registrirame eksterni provajderi). Runtime proverki idat preku IFeatureManager.
            var features = new FeatureOptions();
            builder.Configuration.GetSection("Features").Bind(features);

            var keysPath = builder.Configuration["DataProtection:KeysPath"]
            ?? throw new InvalidOperationException("DataProtection:KeysPath is not configured.");

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("AspNetCoreMvcTemplate");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // So ova se dodava authentication services, user manager, sign-in manager, role manager, identity cookies...
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // Ako EmailConfirmation feature-ot e isklucen, dozvoluvame
                // sign-in bez potvrden email (i Register flow ne prakja confirmation email).
                options.SignIn.RequireConfirmedEmail = features.EmailConfirmation;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

            builder.Services.AddAuthorizationPolicies();

            //var authBuilder = services.AddAuthentication() ova samo extends the existing authentication setup, ne go pregazuva.
            // Hard switch: koga ExternalLogins e isklucen, voopsto ne registriratame
            // provajderi - dury i ako se konfigurirani ClientId/AppId vo settings.
            if (features.ExternalLogins)
            {
                builder.Services.AddExternalAuthentication(builder.Configuration);
            }

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            });

            var app = builder.Build();

            await app.SeedAdminBootstrapAsync();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Error/{0}");

            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapHealthChecks("/health");

            await app.RunAsync();
        }
    }
}
