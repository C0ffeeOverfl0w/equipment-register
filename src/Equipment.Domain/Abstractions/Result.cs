namespace Equipment.Domain.Abstractions;

/// <summary>
/// Представляет успешное завершение операции или ожидаемую ошибку
/// </summary>
public class Result
{
    protected Result(
        bool isSuccess,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Success must have no error; failure must have an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success()
    {
        return new(true, Error.None);
    }

    public static Result Failure(
        Error error)
    {
        return new(false, error);
    }

    public static Result<T> Success<T>(
        T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(value, true, Error.None);
    }

    public static Result<T> Failure<T>(
        Error error)
    {
        return new(default, false, error);
    }

    public static Result<T> Create<T>(
        T? value)
    {
        return value is not null ? Success(value) : Failure<T>(Error.NullValue);
    }
}
