using Finch.Console.Validation;

namespace Finch.Console.UnitTests.Validation;

public static class LoanApplicationFieldValidatorTests
{
    public abstract class LoanApplicationFieldValidatorTestsBase()
    {
        protected readonly LoanApplicationFieldValidator Validator = new();
    }

    public class ValidateLoanAmount() : LoanApplicationFieldValidatorTestsBase
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.01)]
        [InlineData(0.005)]
        public void ShouldBeInvalid_WhenLoanAmountIsBelowOnePenny(decimal loanAmount)
        {
            var result = Validator.ValidateLoanAmount(loanAmount);

            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1)]
        [InlineData(100_000)]
        public void ShouldBeValid_WhenLoanAmountIsAtLeastOnePenny(decimal loanAmount)
        {
            var result = Validator.ValidateLoanAmount(loanAmount);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ShouldBeValid_WhenLoanAmountIsExactlyTheMaximum()
        {
            // The maximum is decimal.MaxValue / 10,000 (see LoanApplicationFieldValidator) -
            // computed the same way here rather than duplicating it as a magic literal.
            var result = Validator.ValidateLoanAmount(decimal.MaxValue / 10_000m);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ShouldBeInvalid_WhenLoanAmountIsAboveTheMaximum()
        {
            var result = Validator.ValidateLoanAmount((decimal.MaxValue / 10_000m) + 1m);

            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }
    }

    public class ValidateAssetValue() : LoanApplicationFieldValidatorTestsBase
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.01)]
        [InlineData(0.005)]
        public void ShouldBeInvalid_WhenAssetValueIsBelowOnePenny(decimal assetValue)
        {
            var result = Validator.ValidateAssetValue(assetValue);

            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1)]
        [InlineData(500_000)]
        public void ShouldBeValid_WhenAssetValueIsAtLeastOnePenny(decimal assetValue)
        {
            var result = Validator.ValidateAssetValue(assetValue);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ShouldBeValid_WhenAssetValueHasNoUpperBound()
        {
            // Unlike loan amount, asset value has no ceiling: a large asset value only shrinks
            // LoanToValue toward zero, it can never make it (or anything downstream) overflow.
            var result = Validator.ValidateAssetValue(1_000_000_000_000_000.00m);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
    }

    public class ValidateCreditScore() : LoanApplicationFieldValidatorTestsBase
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1000)]
        [InlineData(1001)]
        public void ShouldBeInvalid_WhenCreditScoreIsOutsideOneToNineNineNine(int creditScore)
        {
            var result = Validator.ValidateCreditScore(creditScore);

            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(500)]
        [InlineData(999)]
        public void ShouldBeValid_WhenCreditScoreIsWithinOneToNineNineNine(int creditScore)
        {
            var result = Validator.ValidateCreditScore(creditScore);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
    }
}