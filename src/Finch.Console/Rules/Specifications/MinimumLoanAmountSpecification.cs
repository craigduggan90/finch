using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>General limit: decline if the loan amount is below £100,000.</summary>
public class MinimumLoanAmountSpecification : ISpecification<LoanApplication>
{
    private const decimal MinimumLoanAmount = 100_000m;

    public int Order => 1;

    public string Description => "Loan amount must be at least £100,000.";

    public bool IsApplicableTo(LoanApplication item) => true;

    public bool IsSatisfiedBy(LoanApplication item) => item.LoanAmount >= MinimumLoanAmount;
}