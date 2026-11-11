namespace Equipment.Application.Abstractions.Behaviors;

/// <summary>
/// Записывает имя, длительность и исход сценария без содержимого запроса
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing request {RequestName}", requestName);

        try
        {
            TResponse result = await next(cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Request {RequestName} succeeded in {ElapsedMilliseconds} ms",
                    requestName, stopwatch.Elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation("Request {RequestName} rejected with {ErrorCode} in {ElapsedMilliseconds} ms",
                    requestName, result.Error.Code, stopwatch.Elapsed.TotalMilliseconds);
            }

            return result;
        }
        catch (RequestValidationException exception)
        {
            _logger.LogInformation("Request {RequestName} failed validation for {InvalidFieldCount} fields in {ElapsedMilliseconds} ms",
                requestName, exception.Errors.Count, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Request {RequestName} cancelled after {ElapsedMilliseconds} ms",
                requestName, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        catch (Exception)
        {
            // Само исключение со стеком журналирует глобальный обработчик HTTP
            _logger.LogWarning("Request {RequestName} failed unexpectedly after {ElapsedMilliseconds} ms",
                requestName, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }
}
