using System;
using System.Collections.Generic;
using System.Linq;
using HoaDemo.Api.Models;

namespace HoaDemo.Api.Services
{
    /// <summary>
    /// Late-fee rules:
    ///   * No fee until the grace period (DueDate + GracePeriodDays) has passed.
    ///   * No fee if the assessment was paid in full by the end of the grace period.
    ///   * Otherwise: flat fee + percent of the still-unpaid balance, rounded to cents,
    ///     capped at LateFeeMax (when LateFeeMax &gt; 0).
    /// </summary>
    public static class LateFeeCalculator
    {
        public static decimal OutstandingBalance(Assessment assessment, DateTime asOf)
        {
            var paid = (assessment.Payments ?? Enumerable.Empty<Payment>())
                .Where(p => p.PaidOn.Date <= asOf.Date)
                .Sum(p => p.Amount);

            return Math.Max(0m, assessment.Amount - paid);
        }

        public static decimal CalculateLateFee(Assessment assessment, Association policy, DateTime asOf)
        {
            if (assessment == null) throw new ArgumentNullException(nameof(assessment));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var graceEnd = assessment.DueDate.Date.AddDays(policy.GracePeriodDays);
            if (asOf.Date <= graceEnd)
            {
                return 0m;
            }

            var balance = OutstandingBalance(assessment, graceEnd);
            if (balance <= 0m)
            {
                return 0m;
            }

            var fee = policy.LateFeeFlatAmount + Math.Round(balance * policy.LateFeePercent, 2, MidpointRounding.AwayFromZero);

            if (policy.LateFeeMax > 0m && fee > policy.LateFeeMax)
            {
                fee = policy.LateFeeMax;
            }

            return fee;
        }
    }
}
