namespace Finch.Console.Rules.Specifications;

/// <summary>
/// Threshold values shared across multiple specification classes, kept in one place rather than
/// repeated in each. This is a plain data holder, not a base class - the specifications that use it
/// share a value, not behaviour.
/// </summary>
public static class RuleThresholds
{
    public const decimal HighValueThreshold = 1_000_000m;
}