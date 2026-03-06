# AspNetCoreMvcTemplate

Reusable ASP.NET Core MVC starter template with Identity, Admin area, EF Core, logging, error handling, testing, and scalable project structure.

## Purpose

This repository is a reusable starter template for building real-world ASP.NET Core MVC applications without repeating common setup from scratch in every new project.

The goal is to provide a clean and extensible foundation that already includes the most common application requirements, such as:

- MVC shared layout and structure
- Authentication and authorization
- ASP.NET Core Identity with custom `ApplicationUser`
- EF Core integration
- Admin area for managing users, roles, and claims
- Global error handling and status code pages
- Logging
- Testing setup
- Optional integrations such as localization and external login providers

This template is being built incrementally and documented as a learning + production-ready foundation for future projects.

## Planned Features

- ASP.NET Core MVC project structure
- Shared layout, partials, and validation setup
- EF Core + SQL Server
- ASP.NET Core Identity
- Authentication and authorization
- Admin area
- Role and claim management
- Global exception handling
- Status code handling
- Structured logging with Serilog
- Feature flags
- Localization support
- External login providers
- Unit tests

## Solution Structure

```text
AspNetCoreMvcTemplate
│
├── AspNetCoreMvcTemplate.Web
├── AspNetCoreMvcTemplate.Tests
└── docs
