[![CI](https://github.com/PetkovskiM/AspNetCoreMvcTemplate/actions/workflows/ci.yml/badge.svg)](https://github.com/PetkovskiM/AspNetCoreMvcTemplate/actions/workflows/ci.yml)


# AspNetCoreMvcTemplate

A clean, reusable ASP.NET Core MVC starter template built step by step.

## Goal

This project is meant to become a professional ASP.NET Core MVC template for future projects, with:

- Controllers + Views
- EF Core
- ASP.NET Core Identity
- custom MVC authentication pages
- admin foundation
- reusable supporting services
- clean structure for learning and reuse

## Current Features

- ASP.NET Core MVC app
- EF Core + SQL Server
- ASP.NET Core Identity with custom `ApplicationUser`
- custom MVC account pages
- Login
- Register
- Logout
- AccessDenied
- email confirmation flow
- forgot password flow
- reset password flow
- lockout handling
- resend confirmation email flow
- reusable `AspNetCoreMvcTemplate.Emailing` class library
- logging-based email sender for development/testing
- smoke/integration tests
- controller unit tests
- admin bootstrap seeding for initial admin role and admin user

## CI/CD and Deployment

This project currently includes:

- GitHub Actions CI workflow for restore, build, and test
- GitHub Actions CD workflow for deployment to IIS
- deployment from `develop` to a staging IIS environment
- self-hosted Windows runner for IIS deployment

### Current deployment flow

1. Changes are merged into `develop`
2. GitHub Actions builds, tests, and publishes the web app
3. The published output is uploaded as a workflow artifact
4. A self-hosted Windows runner downloads the artifact
5. The app is deployed to the IIS site folder
6. The site is brought back online and verified

### Current deployment target

- Environment: `Staging`
- IIS site: `AspNetCoreMvcTemplate`
- App pool: `AspNetCoreMvcTemplatePool`
- Deployment folder: `D:\Sites\AspNetCoreMvcTemplate\current`
- Port: `8080`

## Secrets and sensitive configuration

Do not commit secrets such as:
- production or staging database passwords
- SMTP/API credentials
- bootstrap admin passwords

Use appropriate environment-specific configuration and secret storage.

### Migration strategy

EF Core migrations are currently applied manually and explicitly.

For now, migrations are **not**:
- applied automatically on app startup
- applied automatically by the deployment workflow

This is intentional for safety and learning clarity.

## Solution Structure

- `src/AspNetCoreMvcTemplate.Web`
- `src/AspNetCoreMvcTemplate.Emailing`
- `tests/AspNetCoreMvcTemplate.Web.Tests`
- `tests/AspNetCoreMvcTemplate.Emailing.Tests`

## Quick Start

### 1. Configure the database connection

Set the connection string in `appsettings.json`, `appsettings.Development.json`, user secrets, or environment variables.

### 2. Apply migrations

Run:

```bash
dotnet ef database update --project src/AspNetCoreMvcTemplate.Web
