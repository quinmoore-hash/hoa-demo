using System.Linq;
using HoaDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HoaDemo.Api.Data
{
    public class HoaContext : DbContext
    {
        public HoaContext(DbContextOptions<HoaContext> options) : base(options)
        {
        }

        public DbSet<Association> Associations { get; set; }
        public DbSet<Homeowner> Homeowners { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var decimalProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

            foreach (var property in decimalProperties)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
