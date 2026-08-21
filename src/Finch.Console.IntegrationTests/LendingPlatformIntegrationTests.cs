using Finch.Console.DependencyInjection;
using Finch.Console.Domain;
using Finch.Console.IntegrationTests.Fakes;
using Finch.Console.Output;
using Finch.Console.Rules;
using Finch.Console.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace Finch.Console.IntegrationTests;

/// <summary>
/// Pseudo end-to-end tests: resolves the real DI-registered rule collection (all 8 specifications)
/// and the real <see cref="RulesEngine"/>/<see cref="LoanApplicationStatistics"/>, substituting only
/// <see cref="IConsoleWriter"/> with an in-memory fake. The interactive
/// <c>ConsoleLoanApplicationReader</c> is intentionally not exercised here - it's a thin wrapper
/// around <see cref="System.Console.ReadLine"/> with no branching beyond what
/// <c>LoanApplicationFieldValidatorTests</c> already covers.
/// </summary>
public static class LendingPlatformIntegrationTests
{
    public abstract class LendingPlatformIntegrationTestsBase
    {
        protected readonly RulesEngine RulesEngine;
        protected readonly LoanApplicationStatistics Statistics;
        protected readonly ApplicationResultPresenter Presenter;
        protected readonly FakeConsoleWriter Writer;

        protected LendingPlatformIntegrationTestsBase()
        {
            var services = new ServiceCollection().AddLendingPlatform();
            services.AddSingleton<IConsoleWriter, FakeConsoleWriter>();

            var provider = services.BuildServiceProvider();

            RulesEngine = provider.GetRequiredService<RulesEngine>();
            Statistics = provider.GetRequiredService<LoanApplicationStatistics>();
            Presenter = provider.GetRequiredService<ApplicationResultPresenter>();
            Writer = (FakeConsoleWriter)provider.GetRequiredService<IConsoleWriter>();
        }
    }

    public class Evaluate : LendingPlatformIntegrationTestsBase
    {
        [Theory]
        [InlineData(50_000, 100_000, 999, "Loan amount must be at least £100,000.")]
        [InlineData(1_600_000, 2_000_000, 999, "Loan amount must not exceed £1,500,000.")]
        [InlineData(1_200_000, 2_000_000, 900, "High value loans (£1m or more) require a credit score of at least 950.")]
        [InlineData(950_000, 1_000_000, 999, "Loans under £1,000,000 require an LTV below 90%.")]
        [InlineData(700_000, 1_000_000, 750, "Loans under £1,000,000 with LTV between 60% and 80% require a credit score of at least 800.")]
        public void ShouldReturnDeclinedWithExpectedReason_ForEachDeclineCase(decimal loanAmount, decimal assetValue, int creditScore, string expectedReason)
        {
            var application = new LoanApplication(loanAmount, assetValue, creditScore);

            var decision = RulesEngine.Evaluate(application);

            Assert.Equal(DecisionOutcome.Declined, decision.Outcome);
            Assert.Equal(expectedReason, decision.Reason);
        }

        [Theory]
        [InlineData(500_000, 1_000_000, 800)] // low value, band 1 (LTV 50% < 60%)
        [InlineData(1_200_000, 2_000_000, 960)] // high value (LTV 60%, credit score 960)
        [InlineData(850_000, 1_000_000, 920)] // low value, band 3 (LTV 85%)
        public void ShouldReturnApproved_ForEachApproveCase(decimal loanAmount, decimal assetValue, int creditScore)
        {
            var application = new LoanApplication(loanAmount, assetValue, creditScore);

            var decision = RulesEngine.Evaluate(application);

            Assert.Equal(DecisionOutcome.Approved, decision.Outcome);
            Assert.Null(decision.Reason);
        }
    }

    public class AggregateStatistics : LendingPlatformIntegrationTestsBase
    {
        [Fact]
        public void ShouldAggregateAcrossAllApplications_AndWriteDecisionsThroughTheInjectedWriter()
        {
            (decimal LoanAmount, decimal AssetValue, int CreditScore)[] applications =
            [
                (500_000, 1_000_000, 800),  // approved, LTV 50
                (1_200_000, 2_000_000, 960), // approved, LTV 60
                (1_200_000, 2_000_000, 900), // declined (high value credit score), LTV 60
                (50_000, 100_000, 999),     // declined (min loan amount), LTV 50
                (1_600_000, 2_000_000, 999), // declined (max loan amount), LTV 80
                (950_000, 1_000_000, 999),  // declined (low value max LTV), LTV 95
                (700_000, 1_000_000, 750),  // declined (low value band 2 credit score), LTV 70
                (850_000, 1_000_000, 920),  // approved, LTV 85
            ];

            foreach (var (loanAmount, assetValue, creditScore) in applications)
            {
                var application = new LoanApplication(loanAmount, assetValue, creditScore);
                var decision = RulesEngine.Evaluate(application);
                Statistics.Record(application, decision);
                Presenter.Present(application, decision, Statistics);
            }

            Assert.Equal(8, Statistics.TotalApplicants);
            Assert.Equal(3, Statistics.ApprovedCount);
            Assert.Equal(5, Statistics.DeclinedCount);
            Assert.Equal(2_550_000m, Statistics.TotalValueWritten);
            Assert.Equal(68.75m, Statistics.MeanLoanToValue, precision: 10);

            Assert.Contains(Writer.Lines, line => line.Contains("APPROVED"));
            Assert.Contains(Writer.Lines, line => line.Contains("DECLINED"));
        }
    }
}