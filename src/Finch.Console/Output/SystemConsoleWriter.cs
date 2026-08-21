namespace Finch.Console.Output;

/// <summary>The sole class permitted to call <see cref="System.Console"/> output methods.</summary>
public class SystemConsoleWriter : IConsoleWriter
{
    public void Write(string message) => System.Console.Write(message);

    public void WriteLine(string message = "") => System.Console.WriteLine(message);
}