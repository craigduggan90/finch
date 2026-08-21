using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class LowValueLtvBand1CreditScoreSpecificationTests
{
    public abstract class LowValueLtvBand1CreditScoreSpecificationTestsBase()
    {
        protected readonly LowValueLtvBand1CreditScoreSpecification Specification = new();
    }

    public class IsApplicableTo() : LowValueLtvBand1CreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeFalse_WhenLoanAmountIsExactlyOneMillion()
        {
            var application = new LoanApplication(LoanAmount: 1_000_000, AssetValue: 5_000_000, CreditScore: 750);

            Assert.False(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeFalse_WhenLtvIsExactlySixty()
        {
            var application = new LoanApplication(LoanAmount: 600_000, AssetValue: 1_000_000, CreditScore: 750);

            Assert.False(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeTrue_WhenLtvIsJustBelowSixty_AndLoanAmountIsUnderOneMillion()
        {
            var application = new LoanApplication(LoanAmount: 599_999, AssetValue: 1_000_000, CreditScore: 750);

            Assert.True(Specification.IsApplicableTo(application));
        }
    }

    public class IsSatisfiedBy() : LowValueLtvBand1CreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeSatisfied_WhenCreditScoreIsExactlySevenFifty()
        {
            var application = new LoanApplication(LoanAmount: 500_000, AssetValue: 1_000_000, CreditScore: 750);

            Assert.True(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldNotBeSatisfied_WhenCreditScoreIsOneBelowSevenFifty()
        {
            var application = new LoanApplication(LoanAmount: 500_000, AssetValue: 1_000_000, CreditScore: 749);

            Assert.False(Specification.IsSatisfiedBy(application));
        }
    }
}