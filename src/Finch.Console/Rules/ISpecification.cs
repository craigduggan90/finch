namespace Finch.Console.Rules;

public interface ISpecification<T>
{
    /// <summary>
    /// Determines this rule's position in evaluation order (ascending) within <see cref="Rules.RulesEngine"/>,
    /// independent of DI registration order - so evaluation order is an explicit, stable contract.
    /// Should be unique across all registered specifications — <see cref="RulesEngine"/> does not enforce this.
    /// </summary>
    int Order { get; }

    /// <summary>Whether this rule should be evaluated for the given item.</summary>
    bool IsApplicableTo(T item);

    /// <summary>Whether the item satisfies this rule. Only meaningful when <see cref="IsApplicableTo"/> is true.</summary>
    bool IsSatisfiedBy(T item);

    /// <summary>Plain-text description of the rule, shown to the user when it fails.</summary>
    string Description { get; }
}