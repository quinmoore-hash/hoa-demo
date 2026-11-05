# HOA Demo – Homeowners Association Management

An internal HOA management app: an ASP.NET Core Web API for associations, homeowners,
dues assessments, and payments (late fees computed on the fly from each association's
policy), plus a Blazor Server UI that staff sign in to for day-to-day management.

**Stack:**

| Concern        | Technology                                                        |
| -------------- | ----------------------------------------------------------------- |
| Runtime        | .NET 8 (cross-platform)                                           |
| API            | ASP.NET Core 8 Web API                                            |
| Data access    | EF Core 8 (SQL Server or SQLite) with migrations                  |
| Serialization  | System.Text.Json                                                  |
| API docs       | Swashbuckle (Swagger UI)                                          |
| UI             | Blazor Server (`HoaDemo.Web`) + ASP.NET Core Identity             |
| Tests          | xUnit + `WebApplicationFactory` (SQLite in-memory)                |

## Layout

```
HoaDemo.sln
src/HoaDemo.Api/
  Controllers/        Associations, Homeowners, Assessments, Payments
  Data/               HoaContext / SqliteHoaContext, seeder, design-time factories
    Migrations/       SqlServer/ and Sqlite/ migration sets
  Models/             Association, Homeowner, Assessment, Payment
  Services/           LateFeeCalculator
  Program.cs          Minimal hosting, provider switch, Swagger
  appsettings*.json   Database:Provider + ConnectionStrings:HoaContext
src/HoaDemo.Web/
  Components/         Blazor pages, layout, Identity account pages
  Data/               ApplicationDbContext (Identity, SQLite) + migrations
  Services/           HoaApiClient (typed HttpClient for the API) + DTOs
  wwwroot/            app.css, css/brand.css, img/logo.svg
tests/HoaDemo.Tests/
  LateFeeCalculator*Tests.cs      Unit tests for the late-fee policy
  Integration/                    WebApplicationFactory API tests
```

## Late-fee rules

Each `Association` carries its own policy (`GracePeriodDays`, `LateFeeFlatAmount`,
`LateFeePercent`, `LateFeeMax`). For a given assessment and "as of" date:

1. No fee until the grace period (`DueDate + GracePeriodDays`) has passed.
2. No fee if the assessment was paid in full by the end of the grace period.
3. Otherwise `fee = LateFeeFlatAmount + round(unpaidBalance * LateFeePercent, 2)`,
   capped at `LateFeeMax` when that is greater than zero.

See `src/HoaDemo.Api/Services/LateFeeCalculator.cs` and the tests for the exact behaviour.

## Endpoints

| Method | Route                                  | Notes                                           |
| ------ | -------------------------------------- | ----------------------------------------------- |
| GET    | `/api/associations`                    |                                                 |
| GET    | `/api/associations/{id}`               |                                                 |
| POST   | `/api/associations`                    |                                                 |
| GET    | `/api/homeowners?associationId=`       |                                                 |
| GET    | `/api/homeowners/{id}`                 |                                                 |
| POST   | `/api/homeowners`                      |                                                 |
| GET    | `/api/assessments?homeownerId=`        |                                                 |
| GET    | `/api/assessments/{id}`                | Includes `balance`, `lateFee`, `totalDue` today |
| POST   | `/api/assessments`                     |                                                 |
| GET    | `/api/payments?assessmentId=`          |                                                 |
| POST   | `/api/payments`                        | `paidOn` defaults to today                      |

## Running locally

Prerequisites: the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). No database
server is needed for development – both apps default to SQLite files created on first run.

```bash
# API – http://localhost:5080 (Swagger UI at /swagger)
dotnet run --project src/HoaDemo.Api

# UI – http://localhost:5090 (talks to the API above)
dotnet run --project src/HoaDemo.Web

# Unit + integration tests
dotnet test
```

On first start the API applies its migrations and seeds one association, homeowner, and
assessment. The UI applies the Identity migrations and, in the `Development` environment,
creates the demo staff login from `src/HoaDemo.Web/appsettings.Development.json`
(`DemoStaffUser`: `staff@hoa-demo.local` / `Staff123!`). Sign in with that account, or
register a new one.

### Database provider

The API reads `Database:Provider` (`Sqlite` or `SqlServer`) and `ConnectionStrings:HoaContext`.
`appsettings.json` defaults to SQL Server (LocalDB); `appsettings.Development.json` overrides
to SQLite (`hoa-demo.db`). Override either via configuration or environment variables, e.g.:

```bash
Database__Provider=SqlServer \
ConnectionStrings__HoaContext="Server=localhost,1433;Database=HoaDemo;User Id=sa;Password=...;TrustServerCertificate=True" \
dotnet run --project src/HoaDemo.Api
```

### Migrations

There is one migration set per provider. After changing the model, add a migration to both
(requires `dotnet tool install --global dotnet-ef`):

```bash
cd src/HoaDemo.Api
dotnet ef migrations add <Name> --context HoaContext       --output-dir Data/Migrations/SqlServer
dotnet ef migrations add <Name> --context SqliteHoaContext --output-dir Data/Migrations/Sqlite
```

The UI's Identity schema lives in `src/HoaDemo.Web/Data/Migrations` (`ApplicationDbContext`, SQLite).

## Branding

`src/HoaDemo.Web/wwwroot/img/logo.svg` is a placeholder and `wwwroot/css/brand.css` only
approximates the palette. Swap in the official Vantaca brand assets (logo and exact colours)
before any external use.

## Migration notes

Ported from .NET Framework 4.8 / Web API 2 / EF6. Behavioural differences to be aware of:

* Navigation properties serialize as empty/`null` – EF Core has no lazy loading, so only
  explicitly included data is returned.
* Error bodies keep the Web API 2 `{"message": "..."}` shape.
* The API has no authentication (same as the legacy service); the UI requires an Identity sign-in.

## Known limitations

* The API is unauthenticated and intended for internal/network-restricted use only.
* The UI is Blazor Server with its own SQLite Identity store; there is no shared user directory.
* Model changes require adding migrations for both the SQL Server and SQLite sets.
* Demo credentials are checked into `appsettings.Development.json` and are only created in `Development`.
