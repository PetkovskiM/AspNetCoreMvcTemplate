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
