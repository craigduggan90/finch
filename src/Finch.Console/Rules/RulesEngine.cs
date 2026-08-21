using Finch.Console.Domain;

namespace Finch.Console.Rules;

/// <summary>
/// Evaluates a <see cref="LoanApplication"/> against every applicable specification, sorted by
/// <see cref="ISpecification{T}.Order"/> ascending - independent of DI registration order, so
/// evaluation order (and therefore which reason is reported first) is consistent across calls. An
/// application is approved only if all applicable specifications are satisfied; otherwise the
/// first failing specification's description is returned as the decline reason (see CLAUDE.md -
/// "Future considerations" for the trade-offs of only surfacing the first failure).
/// </summary>
public class RulesEngine(IEnumerable<ISpecification<LoanApplication>> specifications)
{
    private readonly IReadOnlyCollection<ISpecification<LoanApplication>> _specifications =
        specifications.OrderBy(specification => specification.Order).ToList();

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