using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class HighValueCreditScoreSpecificationTests
{
    public abstract class HighValueCreditScoreSpecificationTestsBase()
    {
        protected readonly HighValueCreditScoreSpecification Specification = new();
    }

    public class IsApplicableTo() : HighValueCreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeTrue_WhenLoanAmountIsExactlyOneMillion()
        {
            var application = new LoanApplication(LoanAmount: 1_000_000, AssetValue: 2_000_000, CreditScore: 950);

            Assert.True(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeFalse_WhenLoanAmountIsOneBelowOneMillion()
        {
            var application = new LoanApplication(LoanAmount: 999_999, AssetValue: 2_000_000, CreditScore: 950);

            Assert.False(Specification.IsApplicableTo(application));
        }
    }

    public class IsSatisfiedBy() : HighValueCreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeSatisfied_WhenCreditScoreIsExactlyNineFifty()
        {
            var application = new LoanApplication(LoanAmount: 1_000_000, AssetValue: 2_000_000, CreditScore: 950);

            Assert.True(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldNotBeSatisfied_WhenCreditScoreIsOneBelowNineFifty()
        {
            var application = new LoanApplication(LoanAmount: 1_000_000, AssetValue: 2_000_000, CreditScore: 949);

            Assert.False(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldBeSatisfied_WhenCreditScoreIsAboveNineFifty()
        {
            var application = new LoanApplication(LoanAmount: 1_000_000, AssetValue: 2_000_000, CreditScore: 999);

            Assert.True(Specification.IsSatisfiedBy(application));
        }
    }
}