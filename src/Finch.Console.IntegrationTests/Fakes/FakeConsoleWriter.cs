using Finch.Console.Output;

namespace Finch.Console.IntegrationTests.Fakes;

/// <summary>
/// Captures every line written instead of touching the real console - the substitution point for
/// integration tests, analogous to swapping in a <c>FakeLogger</c> for <c>ILogger</c>.
/// </summary>
public class FakeConsoleWriter : IConsoleWriter
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Write(string message) => _lines.Add(message);

    public void WriteLine(string message = "") => _lines.Add(message);
}