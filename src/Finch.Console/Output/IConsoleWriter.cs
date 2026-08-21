namespace Finch.Console.Output;

/// <summary>
/// The only abstraction any class other than <see cref="SystemConsoleWriter"/> should use to
/// produce output. Lets integration tests substitute a fake and assert on printed content.
/// </summary>
public interface IConsoleWriter
{
    void Write(string message);

    void WriteLine(string message = "");
}