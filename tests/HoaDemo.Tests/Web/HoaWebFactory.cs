extern alias Web;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace HoaDemo.Tests.Web
{
    /// <summary>Boots the Blazor web app in the given environment against its own throwaway SQLite Identity database.</summary>
    public sealed class HoaWebFactory : WebApplicationFactory<Web::Program>
    {
        private readonly string _environment;
        private readonly IReadOnlyDictionary<string, string> _settings;
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hoa-web-test-{Guid.NewGuid():N}.db");

        public HoaWebFactory(string environment, IReadOnlyDictionary<string, string> settings = null)
        {
            _environment = environment;
            _settings = settings ?? new Dictionary<string, string>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
            // Production has no StaffUser unless a test passes one; blank it so Development's demo user doesn't leak in.
            builder.UseSetting("StaffUser:Email", "");
            builder.UseSetting("StaffUser:Password", "");
            builder.UseSetting("Smtp:Host", "");
            foreach (var (key, value) in _settings)
            {
                builder.UseSetting(key, value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                SqliteConnection.ClearAllPools();
                File.Delete(_dbPath);
            }
        }
    }
}
