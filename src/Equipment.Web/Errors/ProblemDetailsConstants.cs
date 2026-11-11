namespace Equipment.Web.Errors;

/// <summary>
/// Общие ключи расширений и значения протокола HTTP-ошибок
/// </summary>
internal static class ProblemDetailsConstants
{
    public const string Code = "code";
    public const string TraceId = "traceId";
    public const string Errors = "errors";
    public const string MediaType = "application/problem+json";
    public const string ValidationCode = ValidationError.DefaultCode;
    public const string UnexpectedErrorCode = "unexpected_error";
    public const string ValidationTitle = "Некорректные данные запроса";
}
