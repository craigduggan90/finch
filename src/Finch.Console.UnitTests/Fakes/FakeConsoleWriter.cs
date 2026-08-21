using Finch.Console.Output;

namespace Finch.Console.UnitTests.Fakes;

/// <summary>
/// Captures every line written instead of touching the real console. Duplicated from
/// <c>Finch.Console.IntegrationTests.Fakes.FakeConsoleWriter</c> rather than shared, since unit
/// tests don't reference the integration test project.
/// </summary>
public class FakeConsoleWriter : IConsoleWriter
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Write(string message) => _lines.Add(message);

    public void WriteLine(string message = "") => _lines.Add(message);
}