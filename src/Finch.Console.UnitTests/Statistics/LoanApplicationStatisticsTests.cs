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
    }
}