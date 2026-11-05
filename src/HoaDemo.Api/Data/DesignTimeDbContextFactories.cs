using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HoaDemo.Api.Data
{
    // Used only by `dotnet ef` so migrations can be added without starting the host:
    //   dotnet ef migrations add X --context HoaContext       --output-dir Data/Migrations/SqlServer
    //   dotnet ef migrations add X --context SqliteHoaContext --output-dir Data/Migrations/Sqlite
    public class HoaContextFactory : IDesignTimeDbContextFactory<HoaContext>
    {
        public HoaContext CreateDbContext(string[] args) =>
            new HoaContext(new DbContextOptionsBuilder<HoaContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=HoaDemo;Integrated Security=True;TrustServerCertificate=True")
                .Options);
    }

    public class SqliteHoaContextFactory : IDesignTimeDbContextFactory<SqliteHoaContext>
    {
        public SqliteHoaContext CreateDbContext(string[] args) =>
            new SqliteHoaContext(new DbContextOptionsBuilder<SqliteHoaContext>()
                .UseSqlite("Data Source=hoa-demo.db")
                .Options);
    }
}
