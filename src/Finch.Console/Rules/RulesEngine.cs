using Finch.Console.Domain;

namespace Finch.Console.Rules;

/// <summary>
/// Evaluates a <see cref="LoanApplication"/> against every applicable specification, in the order
/// they were supplied. An application is approved only if all applicable specifications are
/// satisfied; otherwise the first failing specification's description is returned as the decline
/// reason (see CLAUDE.md - "Future considerations" for the trade-offs of this approach).
/// </summary>
public class RulesEngine(IEnumerable<ISpecification<LoanApplication>> specifications)
{
    private readonly IReadOnlyCollection<ISpecification<LoanApplication>> _specifications = specifications.ToList();

    public LoanDecision Evaluate(LoanApplication application)
    {
        foreach (var specification in _specifications)
        {
            if (!specification.IsApplicableTo(application))
                continue;

            if (!specification.IsSatisfiedBy(application))
                return LoanDecision.Declined(specification.Description);
        }

        return LoanDecision.Approved();
    }
}