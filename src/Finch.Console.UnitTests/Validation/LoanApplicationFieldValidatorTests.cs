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
        public void ShouldBeInvalid_WhenLoanAmountIsZeroOrNegative(decimal loanAmount)
        {
            var result = Validator.ValidateLoanAmount(loanAmount);

            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1)]
        [InlineData(100_000)]
        public void ShouldBeValid_WhenLoanAmountIsGreaterThanZero(decimal loanAmount)
        {
            var result = Validator.ValidateLoanAmount(loanAmount);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ShouldBeValid_WhenLoanAmountIsExactlyTheMaximum()
        {
            // Written as a decimal literal, not [InlineData], so there's no double round-trip at
            // this magnitude to lose precision on the boundary.
            var result = Validator.ValidateLoanAmount(999_999_999_999_999.99m);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ShouldBeInvalid_WhenLoanAmountIsOneCentAboveTheMaximum()
        {
            var result = Validator.ValidateLoanAmount(1_000_000_000_000_000.00m);

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
        public void ShouldBeInvalid_WhenAssetValueIsZeroOrNegative(decimal assetValue)
        {
            var result = Validator.ValidateAssetValue(assetValue);

            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1)]
        [InlineData(500_000)]
        public void ShouldBeValid_WhenAssetValueIsGreaterThanZero(decimal assetValue)
        {
            var result = Validator.ValidateAssetValue(assetValue);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ShouldBeValid_WhenAssetValueIsExactlyTheMaximum()
        {
            var result = Validator.ValidateAssetValue(999_999_999_999_999.99m);

            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ShouldBeInvalid_WhenAssetValueIsOneCentAboveTheMaximum()
        {
            var result = Validator.ValidateAssetValue(1_000_000_000_000_000.00m);

            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
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