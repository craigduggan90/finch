namespace Finch.Console.Validation;

/// <summary>
/// Validates a single applicant-supplied field at a time, so the console reader can re-prompt
/// just the invalid field instead of validating a whole request up front.
/// </summary>
public class LoanApplicationFieldValidator
{
    // A penny - the smallest unit GBP currency actually has, so this is the true minimum rather
    // than just "greater than zero".
    private const decimal MinimumAmount = 0.01m;

    // Derived, not arbitrary: LoanToValue computes (LoanAmount / AssetValue) * 100, i.e.
    // LoanAmount * (100 / AssetValue). Asset value's floor is MinimumAmount (0.01), so the
    // worst case multiplier is 100 / 0.01 = 10,000. Capping loan amount at decimal.MaxValue /
    // 10,000 guarantees that multiplication can never overflow decimal, even at that worst case.
    // Asset value has no ceiling: a large asset value only shrinks LoanToValue toward zero, it
    // can't grow it - the real guard against a runaway LTV is this cap on the numerator.
    private const decimal MaximumAmount = decimal.MaxValue / 10_000m;

    public FieldValidationResult ValidateLoanAmount(decimal loanAmount) => loanAmount switch
    {
        < MinimumAmount => FieldValidationResult.Fail("Loan amount must be at least £0.01."),
        > MaximumAmount => FieldValidationResult.Fail("Loan amount is too large."),
        _ => FieldValidationResult.Ok(),
    };

    public FieldValidationResult ValidateAssetValue(decimal assetValue) =>
        assetValue < MinimumAmount
            ? FieldValidationResult.Fail("Asset value must be at least £0.01.")
            : FieldValidationResult.Ok();

    public FieldValidationResult ValidateCreditScore(int creditScore) => creditScore switch
    {
        < 1 => FieldValidationResult.Fail("Credit score must be at least 1."),
        > 999 => FieldValidationResult.Fail("Credit score must not exceed 999."),
        _ => FieldValidationResult.Ok(),
    };
}