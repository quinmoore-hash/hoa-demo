using HoaDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HoaDemo.Api.Data
{
    /// <summary>Applies migrations (or creates the schema for non-relational providers) and seeds a sample association.</summary>
    public static class HoaDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HoaContext>();

            if (db.Database.IsRelational())
            {
                if (db.Database.IsSqlServer())
                {
                    await BaselineLegacyEf6DatabaseAsync(db, cancellationToken);
                }

                await db.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }

            if (await db.Associations.AnyAsync(cancellationToken))
            {
                return;
            }

            var hoa = new Association
            {
                Name = "Maple Ridge HOA",
                Address = "100 Maple Ridge Dr, Austin, TX 78701"
            };

            var owner = new Homeowner
            {
                Association = hoa,
                FirstName = "Jordan",
                LastName = "Lee",
                Email = "jordan.lee@example.com",
                PropertyAddress = "102 Maple Ridge Dr"
            };

            db.Assessments.Add(new Assessment
            {
                Homeowner = owner,
                Description = "Q1 dues",
                Amount = 300m,
                DueDate = new DateTime(DateTime.Today.Year, 1, 1)
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        // A database created by the legacy EF6 app already has the InitialCreate tables but no EF Core
        // history, so Migrate() would try to create them again. Record InitialCreate as applied instead;
        // later migrations then run normally against the existing tables and data.
        private static async Task BaselineLegacyEf6DatabaseAsync(HoaContext db, CancellationToken cancellationToken)
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                return;
            }

            var history = db.GetService<IHistoryRepository>();
            if (await history.ExistsAsync(cancellationToken))
            {
                return;
            }

            var isLegacy = await db.Database
                .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.__MigrationHistory') IS NOT NULL AND OBJECT_ID(N'dbo.Associations') IS NOT NULL THEN 1 ELSE 0 END AS [Value]")
                .SingleAsync(cancellationToken);
            if (isLegacy == 0)
            {
                return;
            }

            var initialMigration = db.Database.GetMigrations().First();
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                history.GetInsertScript(new HistoryRow(initialMigration, ProductInfo.GetVersion())),
                cancellationToken);
        }
    }
}
