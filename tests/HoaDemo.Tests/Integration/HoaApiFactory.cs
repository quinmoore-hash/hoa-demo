using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HoaDemo.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace HoaDemo.Tests.Integration
{
    /// <summary>
    /// Boots the real API against an EF Core SQLite in-memory database (one open connection per
    /// factory so the schema survives) with a frozen clock. Each test class gets its own factory
    /// and therefore its own freshly migrated + seeded database.
    /// </summary>
    public class HoaApiFactory : WebApplicationFactory<Program>
    {
        /// <summary>"Today" as seen by the API (UTC, and FakeTimeProvider's local zone is UTC).</summary>
        public static readonly DateTime Today = new DateTime(2025, 3, 10);

        public FakeTimeProvider Clock { get; } = new FakeTimeProvider(new DateTimeOffset(Today.AddHours(12), TimeSpan.Zero));

        private readonly SqliteConnection _connection = new SqliteConnection("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(IsDbContextRegistration).ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<HoaContext, SqliteHoaContext>(o => o.UseSqlite(_connection));

                services.RemoveAll(typeof(TimeProvider));
                services.AddSingleton<TimeProvider>(Clock);
            });
        }

        private static bool IsDbContextRegistration(ServiceDescriptor d) =>
            d.ServiceType == typeof(HoaContext)
            || d.ServiceType == typeof(SqliteHoaContext)
            || d.ServiceType == typeof(DbContextOptions)
            || d.ServiceType == typeof(DbContextOptions<HoaContext>)
            || d.ServiceType == typeof(DbContextOptions<SqliteHoaContext>)
            || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1");

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }
    }

    internal static class HttpExtensions
    {
        public static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        public static async Task<JsonElement> GetJsonAsync(this HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.ReadJsonAsync();
        }

        /// <summary>POSTs <paramref name="body"/> and returns the created entity's id (asserting 201 + Location).</summary>
        public static async Task<int> PostCreatedAsync(this HttpClient client, string url, object body)
        {
            var response = await client.PostAsJsonAsync(url, body, Json);
            var json = await response.ReadJsonAsync();

            Xunit.Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            var id = json.GetProperty("id").GetInt32();
            Xunit.Assert.True(id > 0);
            Xunit.Assert.Equal($"{url.TrimStart('/')}/{id}", response.Headers.Location?.OriginalString);
            return id;
        }

        public static string[] PropertyNames(this JsonElement element) =>
            element.EnumerateObject().Select(p => p.Name).ToArray();
    }
}
