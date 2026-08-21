using Finch.Console.Domain;

namespace Finch.Console.UnitTests.Domain;

public static class LoanApplicationTests
{
    public class LoanToValue
    {
        [Theory]
        [InlineData(500_000, 1_000_000, 50)]
        [InlineData(900_000, 1_000_000, 90)]
        [InlineData(1_000_000, 1_000_000, 100)]
        [InlineData(600_000, 1_000_000, 60)]
        [InlineData(1, 3, 33.333333333333333333333333333)]
        public void ShouldEqualLoanAmountAsPercentageOfAssetValue(decimal loanAmount, decimal assetValue, decimal expectedLtv)
        {
            var application = new LoanApplication(loanAmount, assetValue, CreditScore: 500);

            Assert.Equal(expectedLtv, application.LoanToValue, precision: 10);
        }
    }
}