using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace HoaDemo.Tests.Integration
{
    public class AssociationsApiTests : IClassFixture<HoaApiFactory>
    {
        private readonly HttpClient _client;

        public AssociationsApiTests(HoaApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Seeded_association_is_present_with_default_policy()
        {
            var list = await _client.GetJsonAsync("/api/associations");
            var maple = list.EnumerateArray().Single(a => a.GetProperty("name").GetString() == "Maple Ridge HOA");

            Assert.Equal("100 Maple Ridge Dr, Austin, TX 78701", maple.GetProperty("address").GetString());
            Assert.Equal(15, maple.GetProperty("gracePeriodDays").GetInt32());
            Assert.Equal(25m, maple.GetProperty("lateFeeFlatAmount").GetDecimal());
            Assert.Equal(0.05m, maple.GetProperty("lateFeePercent").GetDecimal());
            Assert.Equal(100m, maple.GetProperty("lateFeeMax").GetDecimal());
        }

        [Fact]
        public async Task Post_then_get_by_id_round_trips()
        {
            var id = await _client.PostCreatedAsync("/api/associations", new
            {
                name = "Cedar Park HOA",
                address = "1 Cedar Park Blvd",
                gracePeriodDays = 10,
                lateFeeFlatAmount = 20m,
                lateFeePercent = 0.02m,
                lateFeeMax = 0m
            });

            var json = await _client.GetJsonAsync($"/api/associations/{id}");

            Assert.Equal(id, json.GetProperty("id").GetInt32());
            Assert.Equal("Cedar Park HOA", json.GetProperty("name").GetString());
            Assert.Equal(10, json.GetProperty("gracePeriodDays").GetInt32());
            Assert.Equal(20m, json.GetProperty("lateFeeFlatAmount").GetDecimal());
            Assert.Equal(0.02m, json.GetProperty("lateFeePercent").GetDecimal());
            Assert.Equal(0m, json.GetProperty("lateFeeMax").GetDecimal());
        }

        [Fact]
        public async Task List_is_ordered_by_name()
        {
            await _client.PostCreatedAsync("/api/associations", new { name = "Zeta Commons" });
            await _client.PostCreatedAsync("/api/associations", new { name = "Alpha Estates" });

            var names = (await _client.GetJsonAsync("/api/associations"))
                .EnumerateArray().Select(a => a.GetProperty("name").GetString()).ToList();

            Assert.Equal(names.OrderBy(n => n, System.StringComparer.Ordinal), names);
            Assert.Contains("Alpha Estates", names);
            Assert.Contains("Zeta Commons", names);
        }

        [Fact]
        public async Task Get_unknown_id_returns_404()
        {
            var response = await _client.GetAsync("/api/associations/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Post_without_required_name_returns_400_with_model_state()
        {
            var response = await _client.PostAsJsonAsync("/api/associations", new { address = "nowhere" });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The request is invalid.", json.GetProperty("message").GetString());
            Assert.True(json.GetProperty("modelState").TryGetProperty("Name", out _));
        }
    }
}
