namespace Finch.Console.Validation;

public record FieldValidationResult(bool IsValid, string? ErrorMessage)
{
    public static FieldValidationResult Ok() => new(true, null);

    public static FieldValidationResult Fail(string errorMessage) => new(false, errorMessage);
}