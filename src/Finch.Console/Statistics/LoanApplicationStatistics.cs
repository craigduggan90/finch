using Finch.Console.Domain;

namespace Finch.Console.Statistics;

/// <summary>
/// Mutable, per-run aggregate of every application recorded so far. Intended to be registered as a
/// singleton so a single instance accumulates state across the interactive loop in <c>Program.cs</c>.
/// </summary>
public class LoanApplicationStatistics
{
    private int _approvedCount;
    private int _declinedCount;
    private decimal _totalValueWritten;
    private decimal _meanLoanToValue;

    public int TotalApplicants => _approvedCount + _declinedCount;

    public int ApprovedCount => _approvedCount;

    public int DeclinedCount => _declinedCount;

    public decimal TotalValueWritten => _totalValueWritten;

    public decimal MeanLoanToValue => _meanLoanToValue;

    public void Record(LoanApplication application, LoanDecision decision)
    {
        if (decision.Outcome == DecisionOutcome.Approved)
        {
            _approvedCount++;
            _totalValueWritten += application.LoanAmount;
        }
        else
        {
            _declinedCount++;
        }

        // Incremental mean (see CLAUDE.md) instead of a running sum divided by count: a single
        // LoanToValue can be as large as decimal.MaxValue (LoanApplicationFieldValidator's ceiling
        // is derived to make that possible), so summing every one recorded could overflow even
        // though the mean itself never exceeds the largest individual value seen.
        _meanLoanToValue += (application.LoanToValue - _meanLoanToValue) / TotalApplicants;
    }
}