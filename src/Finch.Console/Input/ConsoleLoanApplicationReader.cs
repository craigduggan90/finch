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
    public LoanApplicationRequest ReadApplication()
    {
        var loanAmount = ReadDecimalField("Loan amount (GBP): ", validator.ValidateLoanAmount);
        var assetValue = ReadDecimalField("Asset value (GBP): ", validator.ValidateAssetValue);
        var creditScore = ReadIntField("Applicant credit score (1-999): ", validator.ValidateCreditScore);

        return new LoanApplicationRequest(loanAmount, assetValue, creditScore);
    }

    public bool ShouldReadAnotherApplication()
    {
        writer.Write("Submit another application? (y/n): ");
        var response = System.Console.ReadLine()?.Trim();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
               || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private decimal ReadDecimalField(string prompt, Func<decimal, FieldValidationResult> validate)
    {
        while (true)
        {
            writer.Write(prompt);
            var raw = System.Console.ReadLine();

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

    private int ReadIntField(string prompt, Func<int, FieldValidationResult> validate)
    {
        while (true)
        {
            writer.Write(prompt);
            var raw = System.Console.ReadLine();

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