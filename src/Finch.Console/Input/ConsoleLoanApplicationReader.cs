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
        var loanAmount = ReadField<decimal>(
            "Loan amount (GBP): ",
            raw => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null,
            "Enter a valid number.",
            validator.ValidateLoanAmount);
        if (loanAmount is null)
            return null;

        var assetValue = ReadField<decimal>(
            "Asset value (GBP): ",
            raw => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null,
            "Enter a valid number.",
            validator.ValidateAssetValue);
        if (assetValue is null)
            return null;

        var creditScore = ReadField<int>(
            "Applicant credit score (1-999): ",
            raw => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null,
            "Enter a whole number.",
            validator.ValidateCreditScore);
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

    /// <summary>
    /// Prompts for one field, re-prompting on a parse or validation failure, until a valid value is
    /// read or input ends (EOF), in which case this returns null rather than looping forever.
    /// </summary>
    private T? ReadField<T>(string prompt, Func<string, T?> tryParse, string parseErrorMessage, Func<T, FieldValidationResult> validate)
        where T : struct
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

            var parsed = tryParse(raw);
            if (parsed is null)
            {
                writer.WriteLine(parseErrorMessage);
                continue;
            }

            var result = validate(parsed.Value);
            if (!result.IsValid)
            {
                writer.WriteLine(result.ErrorMessage!);
                continue;
            }

            return parsed.Value;
        }
    }
}