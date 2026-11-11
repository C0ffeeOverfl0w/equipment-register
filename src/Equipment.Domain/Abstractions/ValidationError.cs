namespace Equipment.Domain.Abstractions;

public sealed record ValidationError : Error
{
    public const string DefaultCode = "validation_error";

    public ValidationError(
        IDictionary<string, string[]> errors)
        : base(DefaultCode, "Некорректные данные запроса", ErrorType.Validation)
    {
        Errors = new ReadOnlyDictionary<string, string[]>(
            errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
