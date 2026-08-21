using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>Loans under £1m with LTV in [60%, 80%) require a credit score of at least 800.</summary>
public class LowValueLtvBand2CreditScoreSpecification : ISpecification<LoanApplication>
{
    private const decimal BandFloorLtv = 60m;
    private const decimal BandCeilingLtv = 80m;
    private const int MinimumCreditScore = 800;

    public int Order => 7;

    public string Description => "Loans under £1,000,000 with LTV between 60% and 80% require a credit score of at least 800.";

    public bool IsApplicableTo(LoanApplication item) =>
        item is
        {
            LoanAmount: < RuleThresholds.HighValueThreshold,
            LoanToValue: >= BandFloorLtv and < BandCeilingLtv
        };

    public bool IsSatisfiedBy(LoanApplication item) => item.CreditScore >= MinimumCreditScore;
}