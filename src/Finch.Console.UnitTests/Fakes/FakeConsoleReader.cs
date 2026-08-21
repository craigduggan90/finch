using Finch.Console.Input;

namespace Finch.Console.UnitTests.Fakes;

/// <summary>
/// Returns each of the given lines in order, then null (EOF) forever after - lets tests simulate
/// input ending partway through a sequence of prompts, exactly like a closed/piped stdin does.
/// </summary>
public class FakeConsoleReader(params string?[] lines) : IConsoleReader
{
    private readonly Queue<string?> _lines = new(lines);

    public string? ReadLine() => _lines.Count > 0 ? _lines.Dequeue() : null;
}