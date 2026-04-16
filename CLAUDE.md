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
- **Auth cookie**: 60-min expiration, sliding, HttpOnly, RequireConfirmedEmail = true
- **Authorization policies**: `Authorization/` folder with `AuthorizationPolicies` constants (`AdminOnly`, `ActiveUser`). Admin controllers use `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]`. Custom `ActiveUserRequirement` + `ActiveUserAuthorizationHandler` demonstrates the requirement/handler pattern with `UserManager` injection.
- **Error handling**: `ErrorController` with ServerError (500) and StatusCodeError (404/403)
- **Logging**: Serilog (Console + File sinks), configured via `appsettings.json`, request logging middleware
- **Health checks**: `/health` endpoint with EF Core database connectivity check
- **Program.cs** uses extension methods: `AddEmailing()`, `AddAdminBootstrap()`, `AddSerilogLogging()`, `AddAuthorizationPolicies()`

## Completed Features

- Full auth flow: Login, Register, Logout, AccessDenied, email confirmation, forgot/reset password, resend confirmation, lockout
- Admin bootstrap seeding
- Admin area with Dashboard, Roles CRUD, Users management, role assignment
- Bootstrap admin role/user protection (cannot delete/rename admin role, cannot lock out admin user)
- Policy-based authorization (named policies + custom requirement/handler)
- Error handling (500, 404, 403)
- Serilog structured logging (Console + File with daily rolling)
- Health check endpoint at `/health`
- GitHub Actions CI (build + test)
- GitHub Actions CD (IIS deployment to staging)
- Data protection keys persisted to file system
- 49+ unit and integration tests

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
