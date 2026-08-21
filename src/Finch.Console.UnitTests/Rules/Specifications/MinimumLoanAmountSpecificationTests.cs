using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class MinimumLoanAmountSpecificationTests
{
    public abstract class MinimumLoanAmountSpecificationTestsBase()
    {
        protected readonly MinimumLoanAmountSpecification Specification = new();
    }

    public class IsApplicableTo() : MinimumLoanAmountSpecificationTestsBase
    {
        [Fact]
        public void ShouldAlwaysBeTrue()
        {
            Assert.True(Specification.IsApplicableTo(new LoanApplication(1, 1, 1)));
        }
    }

    public class IsSatisfiedBy() : MinimumLoanAmountSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeSatisfied_WhenLoanAmountIsExactlyMinimum()
        {
            var application = new LoanApplication(LoanAmount: 100_000, AssetValue: 1_000_000, CreditScore: 500);

            Assert.True(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldNotBeSatisfied_WhenLoanAmountIsOneBelowMinimum()
        {
            var application = new LoanApplication(LoanAmount: 99_999, AssetValue: 1_000_000, CreditScore: 500);

            Assert.False(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldBeSatisfied_WhenLoanAmountIsAboveMinimum()
        {
            var application = new LoanApplication(LoanAmount: 100_001, AssetValue: 1_000_000, CreditScore: 500);

            Assert.True(Specification.IsSatisfiedBy(application));
        }
    }
}