using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class MaximumLoanAmountSpecificationTests
{
    public abstract class MaximumLoanAmountSpecificationTestsBase()
    {
        protected readonly MaximumLoanAmountSpecification Specification = new();
    }

    public class IsApplicableTo() : MaximumLoanAmountSpecificationTestsBase
    {
        [Fact]
        public void ShouldAlwaysBeTrue()
        {
            Assert.True(Specification.IsApplicableTo(new LoanApplication(1, 1, 1)));
        }
    }

    public class IsSatisfiedBy() : MaximumLoanAmountSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeSatisfied_WhenLoanAmountIsExactlyMaximum()
        {
            var application = new LoanApplication(LoanAmount: 1_500_000, AssetValue: 2_000_000, CreditScore: 950);

            Assert.True(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldNotBeSatisfied_WhenLoanAmountIsOneAboveMaximum()
        {
            var application = new LoanApplication(LoanAmount: 1_500_001, AssetValue: 2_000_000, CreditScore: 950);

            Assert.False(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldBeSatisfied_WhenLoanAmountIsBelowMaximum()
        {
            var application = new LoanApplication(LoanAmount: 1_499_999, AssetValue: 2_000_000, CreditScore: 950);

            Assert.True(Specification.IsSatisfiedBy(application));
        }
    }
}