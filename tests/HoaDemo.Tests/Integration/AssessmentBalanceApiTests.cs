using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace HoaDemo.Tests.Integration
{
    /// <summary>
    /// GET /api/assessments/{id} computes balance / lateFee / totalDue "today", which the fixture
    /// freezes at 2025-03-10. Default policy: 15 grace days, $25 flat + 5%, max $100.
    /// </summary>
    public class AssessmentBalanceApiTests : IClassFixture<HoaApiFactory>
    {
        private static readonly DateTime Today = HoaApiFactory.Today;                 // 2025-03-10
        private static readonly DateTime DueWithinGrace = Today.AddDays(-9);          // 2025-03-01, grace ends 03-16
        private static readonly DateTime DuePastGrace = Today.AddDays(-37);           // 2025-02-01, grace ended 02-16

        private readonly HttpClient _client;

        public AssessmentBalanceApiTests(HoaApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> NewAssessmentAsync(decimal amount, DateTime dueDate, params (decimal amount, DateTime paidOn)[] payments)
        {
            var homeownerId = (await _client.GetJsonAsync("/api/homeowners")).EnumerateArray()
                .Single(h => h.GetProperty("lastName").GetString() == "Lee").GetProperty("id").GetInt32();

            var assessmentId = await _client.PostCreatedAsync("/api/assessments", new { homeownerId, description = "Dues", amount, dueDate = dueDate.ToString("yyyy-MM-dd") });
            foreach (var (paid, paidOn) in payments)
            {
                await _client.PostCreatedAsync("/api/payments", new { assessmentId, amount = paid, paidOn = paidOn.ToString("yyyy-MM-dd"), method = "Check", referenceNumber = "ref" });
            }
            return assessmentId;
        }

        private static void AssertTotals(JsonElement json, decimal balance, decimal lateFee)
        {
            Assert.Equal(balance, json.GetProperty("balance").GetDecimal());
            Assert.Equal(lateFee, json.GetProperty("lateFee").GetDecimal());
            Assert.Equal(balance + lateFee, json.GetProperty("totalDue").GetDecimal());
        }

        [Fact]
        public async Task Response_uses_exact_camelCase_property_names()
        {
            var id = await NewAssessmentAsync(300m, DueWithinGrace, (50m, Today.AddDays(-1)));
            var json = await _client.GetJsonAsync($"/api/assessments/{id}");

            Assert.Equal(
                new[] { "id", "homeownerId", "description", "amount", "dueDate", "balance", "lateFee", "totalDue", "payments" },
                json.PropertyNames());

            var payment = Assert.Single(json.GetProperty("payments").EnumerateArray());
            Assert.Equal(new[] { "id", "amount", "paidOn", "method", "referenceNumber" }, payment.PropertyNames());
            Assert.Equal(50m, payment.GetProperty("amount").GetDecimal());
            Assert.Equal(Today.AddDays(-1), payment.GetProperty("paidOn").GetDateTime());
            Assert.Equal("Check", payment.GetProperty("method").GetString());
            Assert.Equal("ref", payment.GetProperty("referenceNumber").GetString());
        }

        [Fact]
        public async Task Before_grace_end_no_fee()
        {
            var id = await NewAssessmentAsync(300m, DueWithinGrace);
            AssertTotals(await _client.GetJsonAsync($"/api/assessments/{id}"), balance: 300m, lateFee: 0m);
        }

        [Fact]
        public async Task After_grace_end_unpaid_charges_flat_plus_percent()
        {
            var id = await NewAssessmentAsync(300m, DuePastGrace);
            AssertTotals(await _client.GetJsonAsync($"/api/assessments/{id}"), balance: 300m, lateFee: 40m);
        }

        [Fact]
        public async Task Partial_payment_within_grace_reduces_balance_and_percent_portion()
        {
            var id = await NewAssessmentAsync(300m, DuePastGrace, (100m, DuePastGrace.AddDays(5)));
            AssertTotals(await _client.GetJsonAsync($"/api/assessments/{id}"), balance: 200m, lateFee: 35m);
        }

        [Fact]
        public async Task Paid_in_full_within_grace_has_no_fee()
        {
            var id = await NewAssessmentAsync(300m, DuePastGrace, (300m, DuePastGrace.AddDays(15)));
            AssertTotals(await _client.GetJsonAsync($"/api/assessments/{id}"), balance: 0m, lateFee: 0m);
        }

        [Fact]
        public async Task Payment_after_grace_clears_balance_but_fee_stays()
        {
            var id = await NewAssessmentAsync(300m, DuePastGrace, (300m, DuePastGrace.AddDays(20)));
            AssertTotals(await _client.GetJsonAsync($"/api/assessments/{id}"), balance: 0m, lateFee: 40m);
        }

        [Fact]
        public async Task Fee_is_capped_at_policy_max()
        {
            var id = await NewAssessmentAsync(5000m, DuePastGrace);
            AssertTotals(await _client.GetJsonAsync($"/api/assessments/{id}"), balance: 5000m, lateFee: 100m);
        }

        [Fact]
        public async Task Future_dated_payment_is_listed_but_not_counted_in_balance()
        {
            var id = await NewAssessmentAsync(300m, DueWithinGrace, (100m, Today.AddDays(10)));
            var json = await _client.GetJsonAsync($"/api/assessments/{id}");

            AssertTotals(json, balance: 300m, lateFee: 0m);
            Assert.Single(json.GetProperty("payments").EnumerateArray());
        }
    }
}
