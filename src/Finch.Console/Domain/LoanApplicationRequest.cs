namespace Finch.Console.Domain;

/// <summary>
/// Raw applicant input. Each field is expected to have already passed
/// <see cref="Finch.Console.Validation.LoanApplicationFieldValidator"/> before this is constructed.
/// </summary>
public record LoanApplicationRequest(decimal LoanAmount, decimal AssetValue, int CreditScore);