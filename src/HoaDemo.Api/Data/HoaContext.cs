using HoaDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HoaDemo.Api.Data
{
    public class HoaContext : DbContext
    {
        public HoaContext(DbContextOptions<HoaContext> options) : base(options)
        {
        }

        protected HoaContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Association> Associations { get; set; }
        public DbSet<Homeowner> Homeowners { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
            base.ConfigureConventions(configurationBuilder);
        }
    }
}
