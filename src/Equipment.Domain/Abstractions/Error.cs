namespace Equipment.Domain.Abstractions;

public record Error(
    string Code,
    string Description,
    ErrorType Type = ErrorType.Failure,
    string? Field = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A non-null result value is required.");
}
