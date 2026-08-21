namespace Finch.Console.Rules;

public interface ISpecification<T>
{
    /// <summary>Whether this rule should be evaluated for the given item.</summary>
    bool IsApplicableTo(T item);

    /// <summary>Whether the item satisfies this rule. Only meaningful when <see cref="IsApplicableTo"/> is true.</summary>
    bool IsSatisfiedBy(T item);

    /// <summary>Plain-text description of the rule, shown to the user when it fails.</summary>
    string Description { get; }
}