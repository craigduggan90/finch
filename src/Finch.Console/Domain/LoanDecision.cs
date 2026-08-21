namespace Finch.Console.Domain;

/// <summary>
/// The result of running a <see cref="LoanApplication"/> through the rules engine.
/// <see cref="Reason"/> is the description of the first failing rule, or null when approved.
/// </summary>
public record LoanDecision(DecisionOutcome Outcome, string? Reason)
{
    public static LoanDecision Approved() => new(DecisionOutcome.Approved, Reason: null);

    public static LoanDecision Declined(string reason) => new(DecisionOutcome.Declined, reason);
}