namespace Equipment.Web.Errors;

/// <summary>
/// Преобразует ошибки pipeline в прежний контракт HTTP-валидации
/// </summary>
internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public ValidationExceptionHandler(
        IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RequestValidationException validation)
        {
            return false;
        }

        var problem = (ValidationProblemDetails)ApiResults.CreateProblem(new ValidationError(validation.Errors.ToDictionary()), httpContext);
        httpContext.Response.StatusCode = problem.Status!.Value;
        bool written = await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(problem,
                options: null,
                contentType: ProblemDetailsConstants.MediaType,
                cancellationToken: cancellationToken);
        }

        return true;
    }
}
