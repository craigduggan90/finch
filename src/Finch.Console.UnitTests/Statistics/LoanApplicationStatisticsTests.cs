using Finch.Console.Domain;
using Finch.Console.Statistics;

namespace Finch.Console.UnitTests.Statistics;

public static class LoanApplicationStatisticsTests
{
    public class Record
    {
        [Fact]
        public void ShouldLeaveAllTotalsAtZero_WhenNothingHasBeenRecorded()
        {
            var statistics = new LoanApplicationStatistics();

            Assert.Equal(0, statistics.TotalApplicants);
            Assert.Equal(0, statistics.ApprovedCount);
            Assert.Equal(0, statistics.DeclinedCount);
            Assert.Equal(0m, statistics.TotalValueWritten);
            Assert.Equal(0m, statistics.MeanLoanToValue);
        }

        [Fact]
        public void ShouldAggregateCountsAndTotalValueAndMeanLtv_WhenGivenAMixOfApprovedAndDeclinedApplications()
        {
            var statistics = new LoanApplicationStatistics();

            // Approved: 500,000 / 1,000,000 = 50% LTV
            statistics.Record(new LoanApplication(500_000, 1_000_000, 800), LoanDecision.Approved());

            // Declined: 950,000 / 1,000,000 = 95% LTV
            statistics.Record(new LoanApplication(950_000, 1_000_000, 500), LoanDecision.Declined("too risky"));

            // Approved: 1,000,000 / 2,000,000 = 50% LTV
            statistics.Record(new LoanApplication(1_000_000, 2_000_000, 950), LoanDecision.Approved());

            Assert.Equal(3, statistics.TotalApplicants);
            Assert.Equal(2, statistics.ApprovedCount);
            Assert.Equal(1, statistics.DeclinedCount);
            Assert.Equal(1_500_000m, statistics.TotalValueWritten);

            // Mean LTV across ALL applications: (50 + 95 + 50) / 3
            Assert.Equal(65m, statistics.MeanLoanToValue, precision: 10);
        }

        [Fact]
        public void ShouldNotContributeToTotalValueWritten_WhenApplicationIsDeclined()
        {
            var statistics = new LoanApplicationStatistics();

            statistics.Record(new LoanApplication(1_400_000, 2_000_000, 500), LoanDecision.Declined("too risky"));

            Assert.Equal(0m, statistics.TotalValueWritten);
            Assert.Equal(1, statistics.DeclinedCount);
        }

        [Fact]
        public void ShouldNotOverflow_WhenAnExtremeLoanToValueIsFollowedByAnotherApplication()
        {
            var statistics = new LoanApplicationStatistics();

            // LoanAmount at the validator's ceiling with AssetValue at its floor (0.01) drives
            // LoanToValue to exactly decimal.MaxValue - the worst case the mean has to handle.
            var extremeApplication = new LoanApplication(decimal.MaxValue / 10_000m, 0.01m, 500);
            Assert.Equal(decimal.MaxValue, extremeApplication.LoanToValue);

            statistics.Record(extremeApplication, LoanDecision.Declined("too risky"));

            // A sum-based mean would compute decimal.MaxValue + 50 here and throw OverflowException.
            statistics.Record(new LoanApplication(500_000, 1_000_000, 800), LoanDecision.Approved());

            // True mean of {decimal.MaxValue, 50} is (decimal.MaxValue + 50) / 2, computed here as
            // decimal.MaxValue / 2 + 25 so the expected-value calculation doesn't overflow either.
            Assert.Equal(decimal.MaxValue / 2m + 25m, statistics.MeanLoanToValue, precision: 10);
        }
    }
}