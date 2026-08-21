using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class HighValueLtvSpecificationTests
{
    public abstract class HighValueLtvSpecificationTestsBase()
    {
        protected readonly HighValueLtvSpecification Specification = new();
    }

    public class IsApplicableTo() : HighValueLtvSpecificationTestsBase
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

    public class IsSatisfiedBy() : HighValueLtvSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeSatisfied_WhenLtvIsExactlySixty()
        {
            var application = new LoanApplication(LoanAmount: 600_000, AssetValue: 1_000_000, CreditScore: 950);

            Assert.True(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldNotBeSatisfied_WhenLtvIsJustAboveSixty()
        {
            var application = new LoanApplication(LoanAmount: 600_001, AssetValue: 1_000_000, CreditScore: 950);

            Assert.False(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldBeSatisfied_WhenLtvIsBelowSixty()
        {
            var application = new LoanApplication(LoanAmount: 500_000, AssetValue: 1_000_000, CreditScore: 950);

            Assert.True(Specification.IsSatisfiedBy(application));
        }
    }
}