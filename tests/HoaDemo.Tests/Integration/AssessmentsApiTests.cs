using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace HoaDemo.Tests.Integration
{
    public class AssessmentsApiTests : IClassFixture<HoaApiFactory>
    {
        private readonly HttpClient _client;

        public AssessmentsApiTests(HoaApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> SeededHomeownerIdAsync() =>
            (await _client.GetJsonAsync("/api/homeowners")).EnumerateArray()
                .Single(h => h.GetProperty("lastName").GetString() == "Lee").GetProperty("id").GetInt32();

        [Fact]
        public async Task Seeded_q1_dues_are_present()
        {
            var homeownerId = await SeededHomeownerIdAsync();
            var list = await _client.GetJsonAsync($"/api/assessments?homeownerId={homeownerId}");
            var dues = list.EnumerateArray().Single(a => a.GetProperty("description").GetString() == "Q1 dues");

            Assert.Equal(300m, dues.GetProperty("amount").GetDecimal());
            var dueDate = dues.GetProperty("dueDate").GetDateTime();
            Assert.Equal(1, dueDate.Month);
            Assert.Equal(1, dueDate.Day);
        }

        [Fact]
        public async Task Post_then_get_by_id_round_trips()
        {
            var homeownerId = await SeededHomeownerIdAsync();
            var id = await _client.PostCreatedAsync("/api/assessments", new
            {
                homeownerId,
                description = "Pool repair special assessment",
                amount = 450.25m,
                dueDate = "2025-06-01"
            });

            var json = await _client.GetJsonAsync($"/api/assessments/{id}");

            Assert.Equal(id, json.GetProperty("id").GetInt32());
            Assert.Equal(homeownerId, json.GetProperty("homeownerId").GetInt32());
            Assert.Equal("Pool repair special assessment", json.GetProperty("description").GetString());
            Assert.Equal(450.25m, json.GetProperty("amount").GetDecimal());
            Assert.Equal(new DateTime(2025, 6, 1), json.GetProperty("dueDate").GetDateTime());
        }

        [Fact]
        public async Task Filter_by_homeowner_and_order_by_due_date()
        {
            var associationId = (await _client.GetJsonAsync("/api/associations")).EnumerateArray().First().GetProperty("id").GetInt32();
            var homeownerId = await _client.PostCreatedAsync("/api/homeowners", new { associationId, firstName = "Filter", lastName = "Me", propertyAddress = "9 Test Ln" });

            await _client.PostCreatedAsync("/api/assessments", new { homeownerId, description = "Later", amount = 10m, dueDate = "2025-09-01" });
            await _client.PostCreatedAsync("/api/assessments", new { homeownerId, description = "Earlier", amount = 10m, dueDate = "2025-02-01" });
            await _client.PostCreatedAsync("/api/assessments", new { homeownerId, description = "Middle", amount = 10m, dueDate = "2025-05-01" });

            var filtered = (await _client.GetJsonAsync($"/api/assessments?homeownerId={homeownerId}")).EnumerateArray().ToList();

            Assert.Equal(new[] { "Earlier", "Middle", "Later" }, filtered.Select(a => a.GetProperty("description").GetString()));
            Assert.All(filtered, a => Assert.Equal(homeownerId, a.GetProperty("homeownerId").GetInt32()));

            var all = (await _client.GetJsonAsync("/api/assessments")).EnumerateArray().Select(a => a.GetProperty("dueDate").GetDateTime()).ToList();
            Assert.True(all.Count >= 4);
            Assert.Equal(all.OrderBy(d => d), all);
        }

        [Fact]
        public async Task Get_unknown_id_returns_404()
        {
            var response = await _client.GetAsync("/api/assessments/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Post_with_unknown_homeowner_returns_400_message()
        {
            var response = await _client.PostAsJsonAsync("/api/assessments", new { homeownerId = 99999, description = "Ghost", amount = 10m, dueDate = "2025-01-01" });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Unknown homeownerId.", json.GetProperty("message").GetString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public async Task Post_with_non_positive_amount_returns_400_message(decimal amount)
        {
            var homeownerId = await SeededHomeownerIdAsync();
            var response = await _client.PostAsJsonAsync("/api/assessments", new { homeownerId, description = "Free", amount, dueDate = "2025-01-01" });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Amount must be positive.", json.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Post_without_required_description_returns_400_with_model_state()
        {
            var homeownerId = await SeededHomeownerIdAsync();
            var response = await _client.PostAsJsonAsync("/api/assessments", new { homeownerId, amount = 10m, dueDate = "2025-01-01" });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The request is invalid.", json.GetProperty("message").GetString());
            Assert.True(json.GetProperty("modelState").TryGetProperty("Description", out _));
        }
    }
}
