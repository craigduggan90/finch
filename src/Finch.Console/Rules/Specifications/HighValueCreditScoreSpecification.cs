using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>High value loans (£1m+) require a credit score of at least 950.</summary>
public class HighValueCreditScoreSpecification : ISpecification<LoanApplication>
{
    private const decimal HighValueThreshold = 1_000_000m;
    private const int MinimumCreditScore = 950;

    public string Description => "High value loans (£1m or more) require a credit score of at least 950.";

    public bool IsApplicableTo(LoanApplication item) => item.LoanAmount >= HighValueThreshold;

    public bool IsSatisfiedBy(LoanApplication item) => item.CreditScore >= MinimumCreditScore;
}