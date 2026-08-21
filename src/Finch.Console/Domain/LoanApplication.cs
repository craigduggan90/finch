namespace Finch.Console.Domain;

public record LoanApplication(decimal LoanAmount, decimal AssetValue, int CreditScore)
{
    public decimal LoanToValue => (LoanAmount / AssetValue) * 100;
}