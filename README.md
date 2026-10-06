# HOA Demo – Homeowners Association Management API

A small ASP.NET Core Web API service for managing homeowners associations: the associations
themselves, their homeowners, dues assessments, and payments against those assessments.
Late fees are computed on the fly from each association's policy.

**Stack:**

| Concern        | Technology                                   |
| -------------- | -------------------------------------------- |
| Runtime        | .NET 8 (cross-platform)                      |
| Web            | ASP.NET Core (controllers)                   |
| Data access    | Entity Framework Core 8 (SQL Server)         |
| Database       | SQL Server (LocalDB by default)              |
| Serialization  | System.Text.Json (camelCase, string enums)   |
| Tests          | xUnit 2.9                                    |

## Layout

```
HoaDemo.sln
src/HoaDemo.Api/
  Controllers/      Associations, Homeowners, Assessments, Payments
  Data/             HoaContext (EF Core DbContext) + seed initializer
  Models/           Association, Homeowner, Assessment, Payment
  Services/         LateFeeCalculator
  Program.cs        Host, DI, JSON settings, DB create + seed
  appsettings.json  Connection string
tests/HoaDemo.Tests/
  LateFeeCalculatorTests.cs
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

Prerequisites: the **.NET 8 SDK** and a SQL Server instance (LocalDB by default).

```bash
dotnet build HoaDemo.sln
dotnet test  HoaDemo.sln
dotnet run --project src/HoaDemo.Api   # http://localhost:5000/api/associations
```

On startup EF Core creates the `HoaDemo` database (`EnsureCreated`) and seeds one association,
homeowner, and assessment. Change `ConnectionStrings:HoaContext` in `appsettings.json` (or set
the `ConnectionStrings__HoaContext` environment variable) to use another SQL Server.

## Known limitations

* `EnsureCreated`; no migrations – drop the database to pick up schema changes.
* No authentication.
