using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>High value loans (£1m+) require an LTV of 60% or less.</summary>
public class HighValueLtvSpecification : HighValueThresholdSpecification, ISpecification<LoanApplication>
{
    private const decimal MaximumLtv = 60m;

    public int Order => 3;

    public string Description => "High value loans (£1m or more) require an LTV of 60% or less.";

    public bool IsApplicableTo(LoanApplication item) => item.LoanAmount >= HighValueThreshold;

    public bool IsSatisfiedBy(LoanApplication item) => item.LoanToValue <= MaximumLtv;
}