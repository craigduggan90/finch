namespace Finch.Console.Validation;

/// <summary>
/// Validates a single applicant-supplied field at a time, so the console reader can re-prompt
/// just the invalid field instead of validating a whole request up front.
/// </summary>
public class LoanApplicationFieldValidator
{
    // 1 quadrillion minus 0.01 - an upper bound comfortably below decimal's actual range, just to
    // reject unrealistic input before it reaches arithmetic like LoanApplication.LoanToValue.
    private const decimal MaximumAmount = 999_999_999_999_999.99m;

    public FieldValidationResult ValidateLoanAmount(decimal loanAmount) => loanAmount switch
    {
        <= 0 => FieldValidationResult.Fail("Loan amount must be greater than zero."),
        > MaximumAmount => FieldValidationResult.Fail("Loan amount must not exceed £999,999,999,999,999.99."),
        _ => FieldValidationResult.Ok(),
    };

    public FieldValidationResult ValidateAssetValue(decimal assetValue) => assetValue switch
    {
        <= 0 => FieldValidationResult.Fail("Asset value must be greater than zero."),
        > MaximumAmount => FieldValidationResult.Fail("Asset value must not exceed £999,999,999,999,999.99."),
        _ => FieldValidationResult.Ok(),
    };

    public FieldValidationResult ValidateCreditScore(int creditScore) =>
        creditScore is >= 1 and <= 999
            ? FieldValidationResult.Ok()
            : FieldValidationResult.Fail("Credit score must be between 1 and 999.");
}