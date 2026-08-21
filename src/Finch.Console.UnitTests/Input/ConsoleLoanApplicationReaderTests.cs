using Finch.Console.Input;
using Finch.Console.UnitTests.Fakes;
using Finch.Console.Validation;

namespace Finch.Console.UnitTests.Input;

public static class ConsoleLoanApplicationReaderTests
{
    public abstract class ConsoleLoanApplicationReaderTestsBase()
    {
        protected readonly LoanApplicationFieldValidator Validator = new();
        protected readonly FakeConsoleWriter Writer = new();

        protected ConsoleLoanApplicationReader CreateReader(params string?[] lines) =>
            new(Validator, new FakeConsoleReader(lines), Writer);
    }

    public class ReadApplication() : ConsoleLoanApplicationReaderTestsBase
    {
        [Fact]
        public void ShouldReturnApplication_WhenAllThreeFieldsAreValidOnFirstTry()
        {
            var reader = CreateReader("500000", "1000000", "800");

            var application = reader.ReadApplication();

            Assert.NotNull(application);
            Assert.Equal(500_000m, application.LoanAmount);
            Assert.Equal(1_000_000m, application.AssetValue);
            Assert.Equal(800, application.CreditScore);
        }

        [Fact]
        public void ShouldReturnApplication_WhenLoanAmountRequiresMultipleAttempts()
        {
            var reader = CreateReader("not-a-number", "-5", "500000", "1000000", "800");

            var application = reader.ReadApplication();

            Assert.NotNull(application);
            Assert.Equal(500_000m, application.LoanAmount);
            Assert.Contains(Writer.Lines, line => line.Contains("Enter a valid number."));
            Assert.Contains(Writer.Lines, line => line.Contains("Loan amount must be at least"));
        }

        [Fact]
        public void ShouldReturnApplication_WhenCreditScoreRequiresMultipleAttempts()
        {
            var reader = CreateReader("500000", "1000000", "not-a-number", "0", "800");

            var application = reader.ReadApplication();

            Assert.NotNull(application);
            Assert.Equal(800, application.CreditScore);
            Assert.Contains(Writer.Lines, line => line.Contains("Enter a whole number."));
            Assert.Contains(Writer.Lines, line => line.Contains("Credit score must be at least"));
        }

        [Fact]
        public void ShouldReturnNull_WhenEofOccursWhileReadingLoanAmount()
        {
            var reader = CreateReader();

            var application = reader.ReadApplication();

            Assert.Null(application);
            Assert.Contains(Writer.Lines, line => line.Contains("No more input received"));
        }

        [Fact]
        public void ShouldReturnNull_WhenEofOccursWhileReadingAssetValue()
        {
            var reader = CreateReader("500000");

            var application = reader.ReadApplication();

            Assert.Null(application);
        }

        [Fact]
        public void ShouldReturnNull_WhenEofOccursWhileReadingCreditScore()
        {
            var reader = CreateReader("500000", "1000000");

            var application = reader.ReadApplication();

            Assert.Null(application);
        }
    }

    public class ShouldReadAnotherApplication() : ConsoleLoanApplicationReaderTestsBase
    {
        [Theory]
        [InlineData("y")]
        [InlineData("Y")]
        [InlineData("yes")]
        [InlineData("YES")]
        [InlineData(" y ")]
        public void ShouldReturnTrue_WhenResponseIsYOrYes(string response)
        {
            var reader = CreateReader(response);

            Assert.True(reader.ShouldReadAnotherApplication());
        }

        [Theory]
        [InlineData("n")]
        [InlineData("no")]
        [InlineData("")]
        [InlineData("blah")]
        public void ShouldReturnFalse_WhenResponseIsAnythingOtherThanYOrYes(string response)
        {
            var reader = CreateReader(response);

            Assert.False(reader.ShouldReadAnotherApplication());
        }

        [Fact]
        public void ShouldReturnFalse_AndPrintMessage_WhenEofOccurs()
        {
            var reader = CreateReader();

            var result = reader.ShouldReadAnotherApplication();

            Assert.False(result);
            Assert.Contains(Writer.Lines, line => line.Contains("No more input received"));
        }
    }
}