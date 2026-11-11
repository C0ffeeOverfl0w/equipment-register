namespace Equipment.Web.Errors;

internal static class ApiResults
{
    public static ObjectResult Problem(
        Error error,
        HttpContext context)
    {
        ProblemDetails problem = CreateProblem(error, context);
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ProblemDetailsConstants.MediaType }
        };
    }

    public static ProblemDetails CreateProblem(
        Error error,
        HttpContext context)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        ProblemDetails problem = error is ValidationError validation
            ? new ValidationProblemDetails(validation.Errors.ToDictionary())
            : error.Type == ErrorType.Validation && error.Field is not null
                ? new ValidationProblemDetails(new Dictionary<string, string[]> { [error.Field] = [error.Description] })
                : new ProblemDetails();

        problem.Status = statusCode;
        problem.Title = error.Type switch
        {
            ErrorType.Validation => ProblemDetailsConstants.ValidationTitle,
            ErrorType.Failure => "Внутренняя ошибка сервера",
            _ => error.Description
        };
        problem.Extensions[ProblemDetailsConstants.Code] = error.Type == ErrorType.Validation ? ProblemDetailsConstants.ValidationCode : error.Code;
        problem.Extensions[ProblemDetailsConstants.TraceId] = context.TraceIdentifier;

        if (error.Type == ErrorType.Conflict && error.Field is not null)
        {
            problem.Extensions[ProblemDetailsConstants.Errors] = new Dictionary<string, string[]> { [error.Field] = [error.Description] };
        }

        return problem;
    }
}
