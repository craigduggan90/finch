namespace Finch.Console.Input;

/// <summary>
/// The only abstraction any class other than <see cref="SystemConsoleReader"/> should use to read
/// console input. Lets tests substitute a fake that simulates queued input or EOF, mirroring
/// <see cref="Finch.Console.Output.IConsoleWriter"/> on the output side.
/// </summary>
public interface IConsoleReader
{
    /// <summary>Returns the next line of input, or null on EOF - same contract as <see cref="System.Console.ReadLine"/>.</summary>
    string? ReadLine();
}