using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class LowValueMaximumLtvSpecificationTests
{
    public abstract class LowValueMaximumLtvSpecificationTestsBase()
    {
        protected readonly LowValueMaximumLtvSpecification Specification = new();
    }

    public class IsApplicableTo() : LowValueMaximumLtvSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeTrue_WhenLoanAmountIsOneBelowOneMillion()
        {
            var application = new LoanApplication(LoanAmount: 999_999, AssetValue: 2_000_000, CreditScore: 900);

            Assert.True(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeFalse_WhenLoanAmountIsExactlyOneMillion()
        {
            var application = new LoanApplication(LoanAmount: 1_000_000, AssetValue: 2_000_000, CreditScore: 900);

            Assert.False(Specification.IsApplicableTo(application));
        }
    }

    public class IsSatisfiedBy() : LowValueMaximumLtvSpecificationTestsBase
    {
        [Fact]
        public void ShouldNotBeSatisfied_WhenLtvIsExactlyNinety()
        {
            var application = new LoanApplication(LoanAmount: 900_000, AssetValue: 1_000_000, CreditScore: 900);

            Assert.False(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldBeSatisfied_WhenLtvIsJustBelowNinety()
        {
            var application = new LoanApplication(LoanAmount: 899_999, AssetValue: 1_000_000, CreditScore: 900);

            Assert.True(Specification.IsSatisfiedBy(application));
        }
    }
}