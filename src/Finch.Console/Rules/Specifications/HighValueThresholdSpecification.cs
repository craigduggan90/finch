namespace Finch.Console.Rules.Specifications;

/// <summary>
/// Shared base for the specifications that key off the £1m boundary between high-value and
/// low-value loans, so that threshold is defined once instead of repeated in each of them.
/// </summary>
public abstract class HighValueThresholdSpecification
{
    protected const decimal HighValueThreshold = 1_000_000m;
}