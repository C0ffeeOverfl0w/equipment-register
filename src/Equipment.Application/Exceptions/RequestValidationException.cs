namespace Equipment.Application.Exceptions;

/// <summary>
/// Передаёт ошибки входного запроса из pipeline на границу представления
/// </summary>
public sealed class RequestValidationException : Exception
{
    public RequestValidationException(
        IDictionary<string, string[]> errors)
        : base("Request validation failed")
    {
        Errors = new ReadOnlyDictionary<string, string[]>(errors);
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
