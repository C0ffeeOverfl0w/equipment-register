namespace Equipment.Web.Logging;

/// <summary>
/// Связывает HTTP-ответ и записи pipeline одним идентификатором запроса
/// </summary>
internal sealed class RequestLoggingScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingScopeMiddleware> _logger;

    public RequestLoggingScopeMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingScopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object>
        {
            [ProblemDetailsConstants.TraceId] = context.TraceIdentifier
        });
        await _next(context);
    }
}
