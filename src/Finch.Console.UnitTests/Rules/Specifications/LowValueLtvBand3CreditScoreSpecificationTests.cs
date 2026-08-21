using Finch.Console.Domain;
using Finch.Console.Rules.Specifications;

namespace Finch.Console.UnitTests.Rules.Specifications;

public static class LowValueLtvBand3CreditScoreSpecificationTests
{
    public abstract class LowValueLtvBand3CreditScoreSpecificationTestsBase()
    {
        protected readonly LowValueLtvBand3CreditScoreSpecification Specification = new();
    }

    public class IsApplicableTo() : LowValueLtvBand3CreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeFalse_WhenLtvIsJustBelowEighty()
        {
            var application = new LoanApplication(LoanAmount: 799_999, AssetValue: 1_000_000, CreditScore: 900);

            Assert.False(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeTrue_WhenLtvIsExactlyEighty()
        {
            var application = new LoanApplication(LoanAmount: 800_000, AssetValue: 1_000_000, CreditScore: 900);

            Assert.True(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeTrue_WhenLtvIsJustBelowNinety()
        {
            var application = new LoanApplication(LoanAmount: 899_999, AssetValue: 1_000_000, CreditScore: 900);

            Assert.True(Specification.IsApplicableTo(application));
        }

        [Fact]
        public void ShouldBeFalse_WhenLtvIsExactlyNinety()
        {
            var application = new LoanApplication(LoanAmount: 900_000, AssetValue: 1_000_000, CreditScore: 900);

            Assert.False(Specification.IsApplicableTo(application));
        }
    }

    public class IsSatisfiedBy() : LowValueLtvBand3CreditScoreSpecificationTestsBase
    {
        [Fact]
        public void ShouldBeSatisfied_WhenCreditScoreIsExactlyNineHundred()
        {
            var application = new LoanApplication(LoanAmount: 850_000, AssetValue: 1_000_000, CreditScore: 900);

            Assert.True(Specification.IsSatisfiedBy(application));
        }

        [Fact]
        public void ShouldNotBeSatisfied_WhenCreditScoreIsOneBelowNineHundred()
        {
            var application = new LoanApplication(LoanAmount: 850_000, AssetValue: 1_000_000, CreditScore: 899);

            Assert.False(Specification.IsSatisfiedBy(application));
        }
    }
}