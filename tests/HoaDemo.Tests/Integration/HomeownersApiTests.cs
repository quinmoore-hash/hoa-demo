using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace HoaDemo.Tests.Integration
{
    public class HomeownersApiTests : IClassFixture<HoaApiFactory>
    {
        private readonly HttpClient _client;

        public HomeownersApiTests(HoaApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> SeededAssociationIdAsync() =>
            (await _client.GetJsonAsync("/api/associations")).EnumerateArray()
                .Single(a => a.GetProperty("name").GetString() == "Maple Ridge HOA").GetProperty("id").GetInt32();

        [Fact]
        public async Task Seeded_homeowner_is_present()
        {
            var associationId = await SeededAssociationIdAsync();
            var list = await _client.GetJsonAsync($"/api/homeowners?associationId={associationId}");
            var jordan = list.EnumerateArray().Single(h => h.GetProperty("lastName").GetString() == "Lee");

            Assert.Equal("Jordan", jordan.GetProperty("firstName").GetString());
            Assert.Equal("jordan.lee@example.com", jordan.GetProperty("email").GetString());
            Assert.Equal("102 Maple Ridge Dr", jordan.GetProperty("propertyAddress").GetString());
            Assert.Equal(associationId, jordan.GetProperty("associationId").GetInt32());
        }

        [Fact]
        public async Task Post_then_get_by_id_round_trips()
        {
            var associationId = await SeededAssociationIdAsync();
            var id = await _client.PostCreatedAsync("/api/homeowners", new
            {
                associationId,
                firstName = "Sam",
                lastName = "Rivera",
                email = "sam@example.com",
                propertyAddress = "104 Maple Ridge Dr"
            });

            var json = await _client.GetJsonAsync($"/api/homeowners/{id}");

            Assert.Equal("Sam", json.GetProperty("firstName").GetString());
            Assert.Equal("Rivera", json.GetProperty("lastName").GetString());
            Assert.Equal(associationId, json.GetProperty("associationId").GetInt32());
            Assert.Equal(System.Text.Json.JsonValueKind.Null, json.GetProperty("association").ValueKind);
        }

        [Fact]
        public async Task Filter_by_association_and_order_by_last_then_first_name()
        {
            var otherAssociation = await _client.PostCreatedAsync("/api/associations", new { name = "Other HOA" });
            await _client.PostCreatedAsync("/api/homeowners", new { associationId = otherAssociation, firstName = "Zoe", lastName = "Adams", propertyAddress = "1 A St" });
            await _client.PostCreatedAsync("/api/homeowners", new { associationId = otherAssociation, firstName = "Amy", lastName = "Adams", propertyAddress = "2 A St" });
            await _client.PostCreatedAsync("/api/homeowners", new { associationId = otherAssociation, firstName = "Bob", lastName = "Zimmer", propertyAddress = "3 A St" });

            var filtered = (await _client.GetJsonAsync($"/api/homeowners?associationId={otherAssociation}")).EnumerateArray().ToList();

            Assert.Equal(3, filtered.Count);
            Assert.All(filtered, h => Assert.Equal(otherAssociation, h.GetProperty("associationId").GetInt32()));
            Assert.Equal(new[] { "Amy Adams", "Zoe Adams", "Bob Zimmer" },
                filtered.Select(h => $"{h.GetProperty("firstName").GetString()} {h.GetProperty("lastName").GetString()}"));

            var all = (await _client.GetJsonAsync("/api/homeowners")).EnumerateArray().ToList();
            Assert.True(all.Count >= 4);
        }

        [Fact]
        public async Task Get_unknown_id_returns_404()
        {
            var response = await _client.GetAsync("/api/homeowners/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Post_with_unknown_association_returns_400_message()
        {
            var response = await _client.PostAsJsonAsync("/api/homeowners", new
            {
                associationId = 99999,
                firstName = "Nobody",
                lastName = "Nowhere",
                propertyAddress = "0 Null Rd"
            });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Unknown associationId.", json.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Post_missing_required_fields_returns_400_with_model_state()
        {
            var associationId = await SeededAssociationIdAsync();
            var response = await _client.PostAsJsonAsync("/api/homeowners", new { associationId, firstName = "OnlyFirst" });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The request is invalid.", json.GetProperty("message").GetString());
            var modelState = json.GetProperty("modelState");
            Assert.True(modelState.TryGetProperty("LastName", out _));
            Assert.True(modelState.TryGetProperty("PropertyAddress", out _));
        }
    }
}
