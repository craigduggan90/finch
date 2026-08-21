using Finch.Console.Domain;

namespace Finch.Console.Rules.Specifications;

/// <summary>General limit: decline if the loan amount is above £1.5 million.</summary>
public class MaximumLoanAmountSpecification : ISpecification<LoanApplication>
{
    private const decimal MaximumLoanAmount = 1_500_000m;

    public int Order => 2;

    public string Description => "Loan amount must not exceed £1,500,000.";

    public bool IsApplicableTo(LoanApplication item) => true;

    public bool IsSatisfiedBy(LoanApplication item) => item.LoanAmount <= MaximumLoanAmount;
}