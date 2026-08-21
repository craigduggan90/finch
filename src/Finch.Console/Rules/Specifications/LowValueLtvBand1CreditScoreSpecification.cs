using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>Loans under £1m with LTV below 60% require a credit score of at least 750.</summary>
public class LowValueLtvBand1CreditScoreSpecification : ISpecification<LoanApplication>
{
    private const decimal BandCeilingLtv = 60m;
    private const int MinimumCreditScore = 750;

    public int Order => 6;

    public string Description => "Loans under £1,000,000 with LTV below 60% require a credit score of at least 750.";

    public bool IsApplicableTo(LoanApplication item) =>
        item.LoanAmount < RuleThresholds.HighValueThreshold && item.LoanToValue < BandCeilingLtv;

    public bool IsSatisfiedBy(LoanApplication item) => item.CreditScore >= MinimumCreditScore;
}