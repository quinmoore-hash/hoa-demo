using System;
using HoaDemo.Api.Models;
using HoaDemo.Api.Services;
using Xunit;

namespace HoaDemo.Tests
{
    public class LateFeeCalculatorEdgeCaseTests
    {
        private static readonly DateTime DueDate = new DateTime(2024, 1, 1);

        private static Association Policy(int graceDays = 15, decimal flat = 25m, decimal percent = 0.05m, decimal max = 100m) =>
            new Association
            {
                Name = "Test HOA",
                GracePeriodDays = graceDays,
                LateFeeFlatAmount = flat,
                LateFeePercent = percent,
                LateFeeMax = max
            };

        private static Assessment Assessment(decimal amount) => new Assessment { Amount = amount, DueDate = DueDate };

        private static Assessment Paid(decimal amount, decimal paid, DateTime paidOn)
        {
            var assessment = Assessment(amount);
            assessment.Payments.Add(new Payment { Amount = paid, PaidOn = paidOn });
            return assessment;
        }

        // --- grace-period boundary -------------------------------------------------

        [Fact]
        public void On_grace_end_day_fee_is_zero_and_one_day_later_fee_applies()
        {
            Assert.Equal(0m, LateFeeCalculator.CalculateLateFee(Assessment(300m), Policy(), DueDate.AddDays(15)));
            Assert.Equal(40m, LateFeeCalculator.CalculateLateFee(Assessment(300m), Policy(), DueDate.AddDays(16)));
        }

        [Fact]
        public void Zero_grace_period_charges_the_day_after_the_due_date()
        {
            var policy = Policy(graceDays: 0);

            Assert.Equal(0m, LateFeeCalculator.CalculateLateFee(Assessment(300m), policy, DueDate));
            Assert.Equal(40m, LateFeeCalculator.CalculateLateFee(Assessment(300m), policy, DueDate.AddDays(1)));
        }

        [Fact]
        public void Paid_in_full_exactly_on_grace_end_date_waives_fee()
        {
            var assessment = Paid(300m, 300m, DueDate.AddDays(15));
            Assert.Equal(0m, LateFeeCalculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(60)));
        }

        [Fact]
        public void Paid_in_full_one_day_after_grace_end_does_not_waive_fee()
        {
            var assessment = Paid(300m, 300m, DueDate.AddDays(16));
            Assert.Equal(40m, LateFeeCalculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(60)));
        }

        // --- balance -------------------------------------------------------------

        [Fact]
        public void Overpayment_yields_zero_balance_not_negative()
        {
            var assessment = Paid(300m, 350m, DueDate);

            Assert.Equal(0m, LateFeeCalculator.OutstandingBalance(assessment, DueDate));
            Assert.Equal(0m, LateFeeCalculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(60)));
        }

        [Fact]
        public void Null_payments_collection_is_treated_as_unpaid()
        {
            var assessment = new Assessment { Amount = 300m, DueDate = DueDate, Payments = null };
            Assert.Equal(300m, LateFeeCalculator.OutstandingBalance(assessment, DueDate));
        }

        // --- rounding --------------------------------------------------------------

        [Fact]
        public void Percent_rounds_midpoint_away_from_zero()
        {
            // 5% of 100.50 = 5.025 -> 5.03 (banker's rounding would give 5.02); + 25 = 30.03
            Assert.Equal(30.03m, LateFeeCalculator.CalculateLateFee(Assessment(100.50m), Policy(), DueDate.AddDays(16)));

            // 5% of 100.30 = 5.015 -> 5.02 (banker's rounding would also give 5.02); + 25 = 30.02
            Assert.Equal(30.02m, LateFeeCalculator.CalculateLateFee(Assessment(100.30m), Policy(), DueDate.AddDays(16)));
        }

        // --- cap -------------------------------------------------------------------

        [Fact]
        public void Fee_exactly_equal_to_max_is_not_reduced()
        {
            // 25 + 5% of 1500 = 100 == max
            Assert.Equal(100m, LateFeeCalculator.CalculateLateFee(Assessment(1500m), Policy(), DueDate.AddDays(16)));
        }

        [Fact]
        public void Fee_below_max_is_unchanged()
        {
            // 25 + 5% of 1499 = 99.95
            Assert.Equal(99.95m, LateFeeCalculator.CalculateLateFee(Assessment(1499m), Policy(), DueDate.AddDays(16)));
        }

        [Fact]
        public void Fee_above_max_is_capped()
        {
            // 25 + 5% of 1501 = 100.05 -> 100
            Assert.Equal(100m, LateFeeCalculator.CalculateLateFee(Assessment(1501m), Policy(), DueDate.AddDays(16)));
        }

        [Fact]
        public void Max_of_zero_disables_the_cap()
        {
            Assert.Equal(100.05m, LateFeeCalculator.CalculateLateFee(Assessment(1501m), Policy(max: 0m), DueDate.AddDays(16)));
        }

        // --- policy shapes -----------------------------------------------------------

        [Fact]
        public void Flat_only_policy_charges_the_flat_amount()
        {
            var policy = Policy(flat: 30m, percent: 0m, max: 0m);
            Assert.Equal(30m, LateFeeCalculator.CalculateLateFee(Assessment(300m), policy, DueDate.AddDays(16)));
        }

        [Fact]
        public void Percent_only_policy_charges_percent_of_balance()
        {
            var policy = Policy(flat: 0m, percent: 0.10m, max: 0m);
            Assert.Equal(30m, LateFeeCalculator.CalculateLateFee(Assessment(300m), policy, DueDate.AddDays(16)));
        }

        [Fact]
        public void Zero_policy_charges_nothing()
        {
            var policy = Policy(flat: 0m, percent: 0m, max: 0m);
            Assert.Equal(0m, LateFeeCalculator.CalculateLateFee(Assessment(300m), policy, DueDate.AddDays(16)));
        }

        // --- arguments -----------------------------------------------------------------

        [Fact]
        public void Null_assessment_throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => LateFeeCalculator.CalculateLateFee(null, Policy(), DueDate));
            Assert.Equal("assessment", ex.ParamName);
        }

        [Fact]
        public void Null_policy_throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => LateFeeCalculator.CalculateLateFee(Assessment(300m), null, DueDate));
            Assert.Equal("policy", ex.ParamName);
        }

        // --- time component --------------------------------------------------------------

        [Fact]
        public void AsOf_time_component_is_ignored()
        {
            var lastGraceMoment = DueDate.AddDays(15).AddHours(23).AddMinutes(59).AddSeconds(59);
            var firstLateMoment = DueDate.AddDays(16).AddSeconds(1);

            Assert.Equal(0m, LateFeeCalculator.CalculateLateFee(Assessment(300m), Policy(), lastGraceMoment));
            Assert.Equal(40m, LateFeeCalculator.CalculateLateFee(Assessment(300m), Policy(), firstLateMoment));
        }

        [Fact]
        public void Payment_time_component_is_ignored_for_balance()
        {
            var assessment = Paid(300m, 300m, DueDate.AddDays(15).AddHours(18));

            Assert.Equal(0m, LateFeeCalculator.OutstandingBalance(assessment, DueDate.AddDays(15)));
            Assert.Equal(0m, LateFeeCalculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(60)));
        }

        [Fact]
        public void Instance_methods_match_static_methods()
        {
            ILateFeeCalculator calculator = new LateFeeCalculator();
            var assessment = Paid(300m, 100m, DueDate.AddDays(5));

            Assert.Equal(200m, calculator.OutstandingBalance(assessment, DueDate.AddDays(16)));
            Assert.Equal(35m, calculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(16)));
        }
    }
}
