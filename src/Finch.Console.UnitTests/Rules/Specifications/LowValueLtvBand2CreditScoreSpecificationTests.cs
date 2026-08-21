using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class LowValueLtvBand2CreditScoreSpecificationTests
{
    public abstract class LowValueLtvBand2CreditScoreSpecificationTestsBase()
    {
        protected readonly LowValueLtvBand2CreditScoreSpecification Specification = new();
    }

    public class IsApplicableTo() : LowValueLtvBand2CreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeFalse_WhenLtvIsJustBelowSixty()
        {
            var application = new LoanApplication(LoanAmount: 599_999, AssetValue: 1_000_000, CreditScore: 800);

            Assert.False(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeTrue_WhenLtvIsExactlySixty()
        {
            var application = new LoanApplication(LoanAmount: 600_000, AssetValue: 1_000_000, CreditScore: 800);

            Assert.True(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeTrue_WhenLtvIsJustBelowEighty()
        {
            var application = new LoanApplication(LoanAmount: 799_999, AssetValue: 1_000_000, CreditScore: 800);

            Assert.True(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeFalse_WhenLtvIsExactlyEighty()
        {
            var application = new LoanApplication(LoanAmount: 800_000, AssetValue: 1_000_000, CreditScore: 800);

            Assert.False(Specification.IsApplicableTo(application));
        }
    }

    public class IsSatisfiedBy() : LowValueLtvBand2CreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeSatisfied_WhenCreditScoreIsExactlyEightHundred()
        {
            var application = new LoanApplication(LoanAmount: 700_000, AssetValue: 1_000_000, CreditScore: 800);

            Assert.True(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldNotBeSatisfied_WhenCreditScoreIsOneBelowEightHundred()
        {
            var application = new LoanApplication(LoanAmount: 700_000, AssetValue: 1_000_000, CreditScore: 799);

            Assert.False(Specification.IsSatisfiedBy(application));
        }
    }
}