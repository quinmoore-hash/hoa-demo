using System;
using HoaDemo.Api.Models;
using HoaDemo.Api.Services;
using Xunit;

namespace HoaDemo.Tests
{
    public class LateFeeCalculatorTests
    {
        private static readonly DateTime DueDate = new DateTime(2024, 1, 1);

        private static Association Policy() => new Association
        {
            Name = "Test HOA",
            GracePeriodDays = 15,
            LateFeeFlatAmount = 25m,
            LateFeePercent = 0.05m,
            LateFeeMax = 100m
        };

        private static Assessment Assessment(decimal amount) => new Assessment
        {
            Amount = amount,
            DueDate = DueDate
        };

        [Fact]
        public void No_fee_before_due_date()
        {
            var fee = LateFeeCalculator.CalculateLateFee(Assessment(300m), Policy(), DueDate.AddDays(-1));
            Assert.Equal(0m, fee);
        }

        [Fact]
        public void No_fee_on_last_day_of_grace_period()
        {
            var fee = LateFeeCalculator.CalculateLateFee(Assessment(300m), Policy(), DueDate.AddDays(15));
            Assert.Equal(0m, fee);
        }

        [Fact]
        public void Flat_plus_percent_of_balance_once_grace_period_passes()
        {
            // 25 flat + 5% of 300 = 40
            var fee = LateFeeCalculator.CalculateLateFee(Assessment(300m), Policy(), DueDate.AddDays(16));
            Assert.Equal(40m, fee);
        }

        [Fact]
        public void No_fee_when_paid_in_full_within_grace_period()
        {
            var assessment = Assessment(300m);
            assessment.Payments.Add(new Payment { Amount = 300m, PaidOn = DueDate.AddDays(10) });

            var fee = LateFeeCalculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(60));
            Assert.Equal(0m, fee);
        }

        [Fact]
        public void Partial_payment_reduces_the_percentage_portion()
        {
            var assessment = Assessment(300m);
            assessment.Payments.Add(new Payment { Amount = 100m, PaidOn = DueDate.AddDays(5) });

            // 25 flat + 5% of remaining 200 = 35
            var fee = LateFeeCalculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(16));
            Assert.Equal(35m, fee);
        }

        [Fact]
        public void Payment_after_grace_period_does_not_waive_fee()
        {
            var assessment = Assessment(300m);
            assessment.Payments.Add(new Payment { Amount = 300m, PaidOn = DueDate.AddDays(20) });

            var fee = LateFeeCalculator.CalculateLateFee(assessment, Policy(), DueDate.AddDays(30));
            Assert.Equal(40m, fee);
        }

        [Fact]
        public void Fee_is_capped_at_max()
        {
            // 25 + 5% of 5000 = 275 -> capped to 100
            var fee = LateFeeCalculator.CalculateLateFee(Assessment(5000m), Policy(), DueDate.AddDays(16));
            Assert.Equal(100m, fee);
        }

        [Fact]
        public void Zero_max_means_no_cap()
        {
            var policy = Policy();
            policy.LateFeeMax = 0m;

            var fee = LateFeeCalculator.CalculateLateFee(Assessment(5000m), policy, DueDate.AddDays(16));
            Assert.Equal(275m, fee);
        }

        [Fact]
        public void Percentage_is_rounded_to_cents()
        {
            // 5% of 123.45 = 6.1725 -> 6.17; + 25 = 31.17
            var fee = LateFeeCalculator.CalculateLateFee(Assessment(123.45m), Policy(), DueDate.AddDays(16));
            Assert.Equal(31.17m, fee);
        }

        [Fact]
        public void Outstanding_balance_ignores_future_payments()
        {
            var assessment = Assessment(300m);
            assessment.Payments.Add(new Payment { Amount = 100m, PaidOn = DueDate.AddDays(5) });
            assessment.Payments.Add(new Payment { Amount = 200m, PaidOn = DueDate.AddDays(40) });

            Assert.Equal(200m, LateFeeCalculator.OutstandingBalance(assessment, DueDate.AddDays(10)));
            Assert.Equal(0m, LateFeeCalculator.OutstandingBalance(assessment, DueDate.AddDays(40)));
        }
    }
}
