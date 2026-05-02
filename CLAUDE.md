# CLAUDE.md

## Project

ASP.NET Core 10 MVC starter template. Controllers + Views, EF Core, ASP.NET Core Identity, SQL Server, Bootstrap 5.

## Solution Structure

- `src/AspNetCoreMvcTemplate.Web` - Main MVC application
- `src/AspNetCoreMvcTemplate.Emailing` - Email service class library
- `tests/AspNetCoreMvcTemplate.Web.Tests` - xUnit + Moq tests
- `tests/AspNetCoreMvcTemplate.Emailing.Tests` - Emailing tests

## Key Architecture

- **Identity**: `AddIdentity<ApplicationUser, IdentityRole>()` with custom `ApplicationUser` (has `Name` property)
- **DbContext**: `ApplicationDbContext : IdentityDbContext<ApplicationUser>` with SQL Server
- **Email**: `IEmailSender` abstraction in Emailing library, logging provider for dev
- **Admin seeding**: `AdminBootstrapSeeder` creates admin role + user on startup (config-driven)
- **Admin area**: MVC Area at `Areas/Admin/` with Dashboard, Roles CRUD, Users management
- **Role management**: RolesController — create, rename, delete roles. Bootstrap admin role is protected.
- **User management**: UsersController — edit user (Name, EmailConfirmed, LockoutEnabled), assign/unassign roles via checkbox list. Bootstrap admin user is protected.
- **User profile**: `ProfileController` (self-service, `[Authorize]`) — view profile, change name / password / email, set password (for users without one — e.g. external-only sign-up), manage linked external logins. Email change uses a confirmation token sent to the *new* address; on confirmation, both `Email` and `UserName` are updated together so password sign-in still works. After mutating actions `signInManager.RefreshSignInAsync(user)` is called so the cookie's claims (`name`, `email_verified`) re-emit fresh values without forcing a full re-login. Linking flow: `LinkLogin` kicks off OAuth challenge tagged with the user id; `LinkLoginCallback` reads the info via `GetExternalLoginInfoAsync(userId)` and calls `userManager.AddLoginAsync`. Unlinking has an orphan-prevention guard: refuses to remove the only sign-in method (no password AND no other external logins).
- **Auth cookie**: 60-min expiration, sliding, HttpOnly, RequireConfirmedEmail = true
- **Authorization policies**: `Authorization/` folder with `AuthorizationPolicies` constants (`AdminOnly`, `RequireEmailConfirmed`, `ActiveUser`). Admin controllers use `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]`. Custom `ActiveUserRequirement` + `ActiveUserAuthorizationHandler` demonstrates the requirement/handler pattern with `UserManager` injection.
- **Claims pipeline**: `ApplicationUserClaimsPrincipalFactory` overrides `UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>` to emit custom claims at sign-in (`email_verified` from `EmailConfirmed`, `name` from `ApplicationUser.Name`). Registered via `.AddClaimsPrincipalFactory<>()` in Identity setup. The `RequireEmailConfirmed` policy consumes the `email_verified` claim.
- **External login providers**: `AddExternalAuthentication()` extension in `Extensions/AuthenticationServiceCollectionExtensions.cs` conditionally registers Google, Microsoft, and Facebook OAuth based on configuration (Google/Microsoft use `ClientId`/`ClientSecret`; Facebook uses `AppId`/`AppSecret` matching Meta's terminology). Both keys must be present or the provider is skipped. If ClientId is empty, the provider is not added and the Login page button does not render - same code path works across dev/staging/prod. Dev config via `dotnet user-secrets`; staging config via GitHub Environment Secrets injected into `web.config` by the CD pipeline. `AccountController.ExternalLogin` / `ExternalLoginCallback` / `ExternalLoginConfirmation` handle the OAuth flow: already-linked users sign in, new users with provider-supplied email get a local account auto-created with `EmailConfirmed = true` (skipping our own email confirmation flow). Claims interop: providers emit the OIDC-standard `email_verified` claim that `ApplicationUserClaimsPrincipalFactory` also emits for local users - same name, same semantics, so `RequireEmailConfirmed` policy works unchanged for both sign-in paths.
- **Error handling**: `ErrorController` with ServerError (500) and StatusCodeError (404/403)
- **Logging**: Serilog (Console + File sinks), configured via `appsettings.json`, request logging middleware
- **Health checks**: `/health` endpoint with EF Core database connectivity check
- **Feature toggles**: `Features/` folder defines `FeatureOptions` (5 bool flags: `AdminArea`, `ProfileManagement`, `Registration`, `ExternalLogins`, `EmailConfirmation`), `IFeatureManager` (`IsEnabled(string)`), `FeatureManager` (reads `IOptions<FeatureOptions>` via reflection), and `FeatureGateAttribute` (action filter, returns 404 when feature off — "this feature doesn't exist in this app" semantics, NOT 403). Wired via `AddFeatureManagement()` in Program.cs. Startup-time decisions (Identity's `RequireConfirmedEmail`, conditional `AddExternalAuthentication` call) bind a local `FeatureOptions` directly from config; runtime decisions (controllers, views) consume `IFeatureManager` via DI. Views (`_LoginPartial`) use `@inject IFeatureManager` to hide nav links for disabled features. Default for all flags is `true` so apps with no `Features` section behave exactly like before.
- **Program.cs** uses extension methods: `AddEmailing()`, `AddAdminBootstrap()`, `AddSerilogLogging()`, `AddAuthorizationPolicies()`, `AddExternalAuthentication()`, `AddFeatureManagement()`

## Completed Features

- Full auth flow: Login, Register, Logout, AccessDenied, email confirmation, forgot/reset password, resend confirmation, lockout
- Admin bootstrap seeding
- Admin area with Dashboard, Roles CRUD, Users management, role assignment
- Bootstrap admin role/user protection (cannot delete/rename admin role, cannot lock out admin user)
- Policy-based authorization (named policies + custom requirement/handler)
- Custom claims pipeline (`UserClaimsPrincipalFactory` emits `email_verified`, `name`)
- External login providers (Google, Microsoft, Facebook) with conditional registration + auto-create local user on first sign-in
- Self-service user profile: change name / password / email (with confirmation), set password for external-only users, link/unlink external providers (with orphan-prevention guard)
- Error handling (500, 404, 403)
- Serilog structured logging (Console + File with daily rolling)
- Health check endpoint at `/health`
- GitHub Actions CI (build + test)
- GitHub Actions CD (IIS deployment to staging)
- Data protection keys persisted to file system
- Feature toggles for AdminArea, ProfileManagement, Registration, ExternalLogins, EmailConfirmation (configurable via `Features` section in appsettings.json)
- 73+ unit and integration tests

## Branching

- `main` - stable baseline
- `develop` - active integration branch (ahead of main)
- Feature branches: `feature/<name>` off develop

## Commands

```bash
# Build
dotnet build ./AspNetCoreMvcTemplate.slnx --configuration Release

# Test
dotnet test ./AspNetCoreMvcTemplate.slnx --configuration Release

# Apply migrations
dotnet ef database update --project src/AspNetCoreMvcTemplate.Web
```

## Conventions

See AGENTS.md for coding conventions. Key points:
- Prefer Controllers + Views over Razor Pages
- Keep Program.cs clean (use extension methods)
- Simple, beginner-friendly code
- Avoid unnecessary abstractions
- Mention clearly whether a migration is needed
- Run `dotnet build` after changes
