using System;
using HoaDemo.Api.Models;

namespace HoaDemo.Api.Data
{
    /// <summary>Creates the database on first run and seeds a sample association.</summary>
    public static class HoaDbInitializer
    {
        public static void Initialize(HoaContext context)
        {
            if (!context.Database.EnsureCreated())
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

            context.Assessments.Add(new Assessment
            {
                Homeowner = owner,
                Description = "Q1 dues",
                Amount = 300m,
                DueDate = new DateTime(DateTime.Today.Year, 1, 1)
            });

            context.SaveChanges();
        }
    }
}
