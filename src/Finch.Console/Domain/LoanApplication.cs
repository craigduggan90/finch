namespace Finch.Console.Domain;

/// <summary>
/// Domain representation of a loan application, mapped from a validated <see cref="LoanApplicationRequest"/>.
/// Assumes <see cref="AssetValue"/> is greater than zero.
/// </summary>
public record LoanApplication(decimal LoanAmount, decimal AssetValue, int CreditScore)
{
    public decimal LoanToValue => (LoanAmount / AssetValue) * 100;

    public static LoanApplication FromRequest(LoanApplicationRequest request) =>
        new(request.LoanAmount, request.AssetValue, request.CreditScore);
}