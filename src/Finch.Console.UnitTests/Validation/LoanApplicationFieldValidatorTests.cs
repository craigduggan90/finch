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