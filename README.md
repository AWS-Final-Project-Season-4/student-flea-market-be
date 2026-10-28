# Student Flea Market Backend

ASP.NET Core 8 REST API for the Student Flea Market platform. The current vertical slice supports authentication, categories, and item listing management.

## Stack

- .NET SDK 8.0.427 (pinned in `global.json`)
- ASP.NET Core Web API
- ASP.NET Core Identity and JWT bearer authentication
- Entity Framework Core 8 and PostgreSQL
- FluentValidation, Serilog, Swagger
- xUnit

## Projects

- `StudentFleaMarket.Domain`: entities and domain enums.
- `StudentFleaMarket.Application`: API contracts, use-case interfaces, validation, and shared exceptions.
- `StudentFleaMarket.Infrastructure`: EF Core, Identity, JWT, and catalog implementations.
- `StudentFleaMarket.API`: controllers, middleware, HTTP pipeline, and configuration.
- `StudentFleaMarket.Tests`: automated tests.

## Local configuration

Development defaults are in `StudentFleaMarket.API/appsettings.Development.json`. Override secrets with environment variables or .NET user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=StudentFleaMarketDb;Username=postgres;Password=<password>" --project StudentFleaMarket.API
dotnet user-secrets set "Jwt:Key" "<at-least-32-byte-secret>" --project StudentFleaMarket.API
```

Production must provide `ConnectionStrings__DefaultConnection` and `Jwt__Key`; no production secret is committed.

## Restore, build, and test

On Windows, use the verification script. It builds with one MSBuild node to avoid package-assets cache contention:

```powershell
.\scripts\verify.ps1
```

Equivalent commands:

```powershell
dotnet tool restore
dotnet msbuild StudentFleaMarket.sln /t:Restore /m:1
dotnet msbuild StudentFleaMarket.sln /t:Build /m:1 /p:Restore=false
dotnet test StudentFleaMarket.Tests\StudentFleaMarket.Tests.csproj --no-build --no-restore
```

## Database

Create/update the PostgreSQL schema:

```powershell
dotnet ef database update --project StudentFleaMarket.Infrastructure --startup-project StudentFleaMarket.API
```

Create a new migration:

```powershell
dotnet ef migrations add <MigrationName> --project StudentFleaMarket.Infrastructure --startup-project StudentFleaMarket.API --output-dir Persistence\Migrations
```

## Run

```powershell
dotnet run --project StudentFleaMarket.API
```

Swagger is enabled in Development. A ready-to-edit request collection is available in `StudentFleaMarket.API/StudentFleaMarket.API.http`.

Main endpoints:

- `GET /health`
- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/categories`
- `GET /api/items`
- `GET /api/items/{id}`
- `POST /api/items` (JWT required)
- `PUT /api/items/{id}` (JWT required, seller only)
- `DELETE /api/items/{id}` (JWT required, seller only)
