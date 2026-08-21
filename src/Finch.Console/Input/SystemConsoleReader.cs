namespace Finch.Console.Input;

/// <summary>The sole class permitted to call <see cref="System.Console.ReadLine"/>.</summary>
public class SystemConsoleReader : IConsoleReader
{
    public string? ReadLine() => System.Console.ReadLine();
}