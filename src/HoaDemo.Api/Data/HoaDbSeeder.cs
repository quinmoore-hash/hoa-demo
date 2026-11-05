using HoaDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

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
    }
}
