using Microsoft.EntityFrameworkCore;

namespace HoaDemo.Api.Data
{
    /// <summary>Same model as <see cref="HoaContext"/>, with its own SQLite migration set.</summary>
    public class SqliteHoaContext : HoaContext
    {
        public SqliteHoaContext(DbContextOptions<SqliteHoaContext> options) : base(options)
        {
        }
    }
}
