# HOA Demo – Homeowners Association Management API

A small ASP.NET Web API 2 service for managing homeowners associations: the associations
themselves, their homeowners, dues assessments, and payments against those assessments.
Late fees are computed on the fly from each association's policy.

**Stack (legacy):**

| Concern        | Technology                                   |
| -------------- | -------------------------------------------- |
| Runtime        | .NET Framework 4.8 (Windows only)            |
| Web            | ASP.NET Web API 2 (`System.Web`, IIS-hosted) |
| Data access    | Entity Framework 6.4 (code-first)            |
| Database       | SQL Server (LocalDB by default)              |
| Serialization  | Newtonsoft.Json 13                           |
| Tests          | xUnit 2.9                                    |

## Layout

```
HoaDemo.sln
src/HoaDemo.Api/
  Controllers/      Associations, Homeowners, Assessments, Payments
  Data/             HoaContext (EF6 DbContext) + seed initializer
  Models/           Association, Homeowner, Assessment, Payment
  Services/         LateFeeCalculator
  App_Start/        WebApiConfig (routing + JSON settings)
  Global.asax(.cs)  Application start
  Web.config        Connection string, EF6 provider, IIS handlers
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

## Running locally (Windows)

Prerequisites: Visual Studio 2022 (or Build Tools) with the **.NET Framework 4.8 targeting
pack**, and **SQL Server Express LocalDB**.

```powershell
dotnet build HoaDemo.sln
dotnet test  HoaDemo.sln
```

Open the solution in Visual Studio and run `HoaDemo.Api` with IIS Express, or point an IIS
site at `src/HoaDemo.Api`. On first request EF6 creates the `HoaDemo` database on
`(localdb)\MSSQLLocalDB` and seeds one association, homeowner, and assessment.
Change the `HoaContext` connection string in `Web.config` to use another SQL Server.

## Known limitations

* Tied to .NET Framework 4.8 / `System.Web` – builds and runs only on Windows.
* EF6 with `CreateDatabaseIfNotExists`; no migrations – drop the database to pick up schema changes.
* Controllers instantiate `HoaContext` directly; there is no DI container.
* No authentication.
