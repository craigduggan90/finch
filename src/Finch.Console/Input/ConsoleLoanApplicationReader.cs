using Finch.Console.Domain;
using Finch.Console.Output;
using Finch.Console.Validation;
using System.Globalization;

namespace Finch.Console.Input;

/// <summary>
/// Interactively prompts for a loan application, one field at a time, re-prompting the same field
/// until it passes validation. Reads directly from <see cref="System.Console.ReadLine"/> - reading
/// input isn't covered by the single-writer constraint, since tests don't assert on it.
/// </summary>
public class ConsoleLoanApplicationReader(LoanApplicationFieldValidator validator, IConsoleWriter writer)
{
    /// <summary>
    /// Prompts for a full application. Returns null if input ends (e.g. EOF on piped/redirected
    /// stdin) before all three fields are answered - there's no valid application to return, and
    /// nothing left to read, so the caller should stop rather than loop forever.
    /// </summary>
    public LoanApplicationRequest? ReadApplication()
    {
        var loanAmount = ReadDecimalField("Loan amount (GBP): ", validator.ValidateLoanAmount);
        if (loanAmount is null)
            return null;

        var assetValue = ReadDecimalField("Asset value (GBP): ", validator.ValidateAssetValue);
        if (assetValue is null)
            return null;

        var creditScore = ReadIntField("Applicant credit score (1-999): ", validator.ValidateCreditScore);
        if (creditScore is null)
            return null;

        return new LoanApplicationRequest(loanAmount.Value, assetValue.Value, creditScore.Value);
    }

    public bool ShouldReadAnotherApplication()
    {
        writer.Write("Submit another application? (y/n): ");
        var response = System.Console.ReadLine()?.Trim();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
               || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private decimal? ReadDecimalField(string prompt, Func<decimal, FieldValidationResult> validate)
    {
        while (true)
        {
            writer.Write(prompt);
            var raw = System.Console.ReadLine();

            if (raw is null)
            {
                writer.WriteLine();
                writer.WriteLine("No more input received - ending session.");
                return null;
            }

            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                writer.WriteLine("Enter a valid number.");
                continue;
            }

            var result = validate(value);
            if (!result.IsValid)
            {
                writer.WriteLine(result.ErrorMessage!);
                continue;
            }

            return value;
        }
    }

    private int? ReadIntField(string prompt, Func<int, FieldValidationResult> validate)
    {
        while (true)
        {
            writer.Write(prompt);
            var raw = System.Console.ReadLine();

            if (raw is null)
            {
                writer.WriteLine();
                writer.WriteLine("No more input received - ending session.");
                return null;
            }

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                writer.WriteLine("Enter a whole number.");
                continue;
            }

            var result = validate(value);
            if (!result.IsValid)
            {
                writer.WriteLine(result.ErrorMessage!);
                continue;
            }

            return value;
        }
    }
}
