using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>Loans under £1m are declined outright once LTV reaches 90%.</summary>
public class LowValueMaximumLtvSpecification : ISpecification<LoanApplication>
{
    private const decimal HighValueThreshold = 1_000_000m;
    private const decimal MaximumLtv = 90m;

    public string Description => "Loans under £1,000,000 require an LTV below 90%.";

    public bool IsApplicableTo(LoanApplication item) => item.LoanAmount < HighValueThreshold;

    public bool IsSatisfiedBy(LoanApplication item) => item.LoanToValue < MaximumLtv;
}