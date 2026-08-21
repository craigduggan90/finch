using Finch.Console.Domain;
using Finch.Console.Statistics;
using System.Globalization;

namespace Finch.Console.Output;

/// <summary>Formats a decision and the running statistics snapshot into console output.</summary>
public class ApplicationResultPresenter(IConsoleWriter writer)
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-GB");

    public void Present(LoanApplication application, LoanDecision decision, LoanApplicationStatistics statistics)
    {
        writer.WriteLine();
        writer.WriteLine($"Loan amount: {FormatCurrency(application.LoanAmount)}");
        writer.WriteLine($"Asset value: {FormatCurrency(application.AssetValue)}");
        writer.WriteLine($"Credit score: {application.CreditScore}");
        writer.WriteLine($"LTV: {FormatPercentage(application.LoanToValue)}");

        writer.WriteLine(decision.Outcome == DecisionOutcome.Approved
            ? "Decision: APPROVED"
            : $"Decision: DECLINED - {decision.Reason}");

        writer.WriteLine();
        writer.WriteLine("--- Summary ---");
        writer.WriteLine($"Total applicants: {statistics.TotalApplicants} " +
                          $"(Approved: {statistics.ApprovedCount}, Declined: {statistics.DeclinedCount})");
        writer.WriteLine($"Total value of loans written: {FormatCurrency(statistics.TotalValueWritten)}");
        writer.WriteLine($"Mean LTV across all applications: {FormatPercentage(statistics.MeanLoanToValue)}");
        writer.WriteLine();
    }

    private static string FormatCurrency(decimal amount) => amount.ToString("C2", Culture);

    private static string FormatPercentage(decimal percentage) => percentage.ToString("N2", Culture) + "%";
}