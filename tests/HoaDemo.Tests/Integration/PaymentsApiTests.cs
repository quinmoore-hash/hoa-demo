using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace HoaDemo.Tests.Integration
{
    public class PaymentsApiTests : IClassFixture<HoaApiFactory>
    {
        private readonly HttpClient _client;

        public PaymentsApiTests(HoaApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> NewAssessmentAsync()
        {
            var homeownerId = (await _client.GetJsonAsync("/api/homeowners")).EnumerateArray().First().GetProperty("id").GetInt32();
            return await _client.PostCreatedAsync("/api/assessments", new { homeownerId, description = "Payable", amount = 300m, dueDate = "2025-02-01" });
        }

        [Fact]
        public async Task Post_without_paidOn_defaults_to_today_from_the_clock()
        {
            var assessmentId = await NewAssessmentAsync();

            var response = await _client.PostAsJsonAsync("/api/payments", new { assessmentId, amount = 50m, method = "ACH" });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(HoaApiFactory.Today, json.GetProperty("paidOn").GetDateTime());
            Assert.Equal(50m, json.GetProperty("amount").GetDecimal());
            Assert.Equal("ACH", json.GetProperty("method").GetString());
        }

        [Fact]
        public async Task Post_with_paidOn_keeps_the_given_date()
        {
            var assessmentId = await NewAssessmentAsync();

            var id = await _client.PostCreatedAsync("/api/payments", new { assessmentId, amount = 75m, paidOn = "2025-01-20", method = "Check", referenceNumber = "1001" });

            var payment = (await _client.GetJsonAsync($"/api/payments?assessmentId={assessmentId}")).EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == id);
            Assert.Equal(new DateTime(2025, 1, 20), payment.GetProperty("paidOn").GetDateTime());
            Assert.Equal("Check", payment.GetProperty("method").GetString());
            Assert.Equal("1001", payment.GetProperty("referenceNumber").GetString());
        }

        [Fact]
        public async Task List_filters_by_assessment_and_orders_newest_first()
        {
            var assessmentId = await NewAssessmentAsync();
            var otherAssessmentId = await NewAssessmentAsync();

            await _client.PostCreatedAsync("/api/payments", new { assessmentId, amount = 10m, paidOn = "2025-01-05" });
            await _client.PostCreatedAsync("/api/payments", new { assessmentId, amount = 20m, paidOn = "2025-03-01" });
            await _client.PostCreatedAsync("/api/payments", new { assessmentId, amount = 30m, paidOn = "2025-02-01" });
            await _client.PostCreatedAsync("/api/payments", new { assessmentId = otherAssessmentId, amount = 99m, paidOn = "2025-02-15" });

            var filtered = (await _client.GetJsonAsync($"/api/payments?assessmentId={assessmentId}")).EnumerateArray().ToList();

            Assert.Equal(3, filtered.Count);
            Assert.All(filtered, p => Assert.Equal(assessmentId, p.GetProperty("assessmentId").GetInt32()));
            Assert.Equal(new[] { 20m, 30m, 10m }, filtered.Select(p => p.GetProperty("amount").GetDecimal()));

            var all = (await _client.GetJsonAsync("/api/payments")).EnumerateArray().Select(p => p.GetProperty("paidOn").GetDateTime()).ToList();
            Assert.True(all.Count >= 4);
            Assert.Equal(all.OrderByDescending(d => d), all);
        }

        [Fact]
        public async Task Post_with_unknown_assessment_returns_400_message()
        {
            var response = await _client.PostAsJsonAsync("/api/payments", new { assessmentId = 99999, amount = 10m });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Unknown assessmentId.", json.GetProperty("message").GetString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Post_with_non_positive_amount_returns_400_message(decimal amount)
        {
            var assessmentId = await NewAssessmentAsync();
            var response = await _client.PostAsJsonAsync("/api/payments", new { assessmentId, amount });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Amount must be positive.", json.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Post_with_malformed_body_returns_400_with_model_state()
        {
            var assessmentId = await NewAssessmentAsync();
            var response = await _client.PostAsJsonAsync("/api/payments", new { assessmentId, amount = "lots" });
            var json = await response.ReadJsonAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The request is invalid.", json.GetProperty("message").GetString());
            Assert.True(json.GetProperty("modelState").EnumerateObject().Any());
        }
    }
}
