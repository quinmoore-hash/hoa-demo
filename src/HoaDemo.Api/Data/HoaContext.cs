using System.Data.Entity;
using HoaDemo.Api.Models;

namespace HoaDemo.Api.Data
{
    public class HoaContext : DbContext
    {
        public HoaContext() : base("name=HoaContext")
        {
        }

        public DbSet<Association> Associations { get; set; }
        public DbSet<Homeowner> Homeowners { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Properties<decimal>().Configure(p => p.HasPrecision(18, 2));
            base.OnModelCreating(modelBuilder);
        }
    }
}
