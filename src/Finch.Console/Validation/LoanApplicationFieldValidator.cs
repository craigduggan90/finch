namespace Finch.Console.Validation;

/// <summary>
/// Validates a single applicant-supplied field at a time, so the console reader can re-prompt
/// just the invalid field instead of validating a whole request up front.
/// </summary>
public class LoanApplicationFieldValidator
{
    public FieldValidationResult ValidateLoanAmount(decimal loanAmount) =>
        loanAmount > 0
            ? FieldValidationResult.Ok()
            : FieldValidationResult.Fail("Loan amount must be greater than zero.");

    public FieldValidationResult ValidateAssetValue(decimal assetValue) =>
        assetValue > 0
            ? FieldValidationResult.Ok()
            : FieldValidationResult.Fail("Asset value must be greater than zero.");

    public FieldValidationResult ValidateCreditScore(int creditScore) =>
        creditScore is >= 1 and <= 999
            ? FieldValidationResult.Ok()
            : FieldValidationResult.Fail("Credit score must be between 1 and 999.");
}