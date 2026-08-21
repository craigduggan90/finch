using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>Loans under £1m with LTV in [80%, 90%) require a credit score of at least 900.</summary>
public class LowValueLtvBand3CreditScoreSpecification : ISpecification<LoanApplication>
{
    private const decimal HighValueThreshold = 1_000_000m;
    private const decimal BandFloorLtv = 80m;
    private const decimal BandCeilingLtv = 90m;
    private const int MinimumCreditScore = 900;

    public int Order => 8;

    public string Description => "Loans under £1,000,000 with LTV between 80% and 90% require a credit score of at least 900.";

    public bool IsApplicableTo(LoanApplication item) =>
        item.LoanAmount < HighValueThreshold
        && item.LoanToValue >= BandFloorLtv
        && item.LoanToValue < BandCeilingLtv;

    public bool IsSatisfiedBy(LoanApplication item) => item.CreditScore >= MinimumCreditScore;
}
