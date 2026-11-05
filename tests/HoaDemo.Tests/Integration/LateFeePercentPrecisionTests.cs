using System.Threading.Tasks;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HoaDemo.Tests.Integration
{
    /// <summary>LateFeePercent is a fraction (0.025 = 2.5%), so unlike money columns it must keep more than 2 decimals.</summary>
    public class LateFeePercentPrecisionTests : IClassFixture<HoaApiFactory>
    {
        private readonly HoaApiFactory _factory;

        public LateFeePercentPrecisionTests(HoaApiFactory factory)
        {
            _factory = factory;
        }

        public static TheoryData<string> Providers => new TheoryData<string> { "SqlServer", "Sqlite" };

        [Theory]
        [MemberData(nameof(Providers))]
        public void Model_maps_LateFeePercent_with_four_decimal_places(string provider)
        {
            using HoaContext db = provider == "SqlServer"
                ? new HoaContextFactory().CreateDbContext(new string[0])
                : new SqliteHoaContextFactory().CreateDbContext(new string[0]);
            var association = db.Model.FindEntityType(typeof(Association))!;

            Assert.Equal(18, association.FindProperty(nameof(Association.LateFeePercent))!.GetPrecision());
            Assert.Equal(4, association.FindProperty(nameof(Association.LateFeePercent))!.GetScale());
            Assert.Equal(2, association.FindProperty(nameof(Association.LateFeeFlatAmount))!.GetScale());
            Assert.Equal(2, association.FindProperty(nameof(Association.LateFeeMax))!.GetScale());
        }

        [Fact]
        public async Task Fractional_percent_round_trips_and_drives_the_late_fee()
        {
            var client = _factory.CreateClient();
            var associationId = await client.PostCreatedAsync("/api/associations",
                new { name = "Half Percent HOA", gracePeriodDays = 0, lateFeeFlatAmount = 0m, lateFeePercent = 0.025m, lateFeeMax = 0m });
            var homeownerId = await client.PostCreatedAsync("/api/homeowners",
                new { associationId, firstName = "Pat", lastName = "Fraction", propertyAddress = "1 Fraction Ct" });
            var assessmentId = await client.PostCreatedAsync("/api/assessments",
                new { homeownerId, description = "Dues", amount = 300m, dueDate = HoaApiFactory.Today.AddDays(-10).ToString("yyyy-MM-dd") });

            var association = await client.GetJsonAsync($"/api/associations/{associationId}");
            Assert.Equal(0.025m, association.GetProperty("lateFeePercent").GetDecimal());

            var assessment = await client.GetJsonAsync($"/api/assessments/{assessmentId}");
            Assert.Equal(7.50m, assessment.GetProperty("lateFee").GetDecimal());   // 300 × 2.5%, not 300 × 3%
        }
    }
}
