# Local and Server Setup

## Local development prerequisites

- .NET SDK 10.0.201
- SQL Server
- Visual Studio / compatible editor
- Git

## Local setup

1. Clone the repository
2. Restore local tools if used
3. Restore NuGet packages
4. Update connection string/user secrets as needed
5. Apply database migrations manually
6. Run the application

## Server / IIS deployment prerequisites

- IIS installed
- .NET 10 Hosting Bundle installed
- IIS app pool created:
  - `AspNetCoreMvcTemplatePool`
  - `.NET CLR Version: No Managed Code`
- IIS site created:
  - `AspNetCoreMvcTemplate`
  - physical path: `D:\Sites\AspNetCoreMvcTemplate\current`
  - port: `8080`
- self-hosted GitHub Actions runner installed on the server
- runner label:
  - `iis-template-server`

## Environment configuration

The deployed IIS app uses:

- `ASPNETCORE_ENVIRONMENT=Staging`

This is currently defined through the project `web.config`, which is included in publish output.

## Deployment workflow summary

The deployment workflow:

- runs on push to `develop`
- can also be triggered manually
- builds/tests/publishes on a GitHub-hosted runner
- deploys on a self-hosted Windows runner
- uses `app_offline.htm` during file copy

## Database migration strategy

Database migrations are currently manual.

They are not executed:
- on application startup
- by the deployment workflow

When schema changes are introduced, apply migrations explicitly as part of deployment.