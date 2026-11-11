namespace Equipment.Domain.Abstractions;

/// <summary>
/// Содержит значение только при успешном завершении операции
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(
        T? value,
        bool isSuccess,
        Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Возвращает успешное значение и запрещает чтение значения при ошибке
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static implicit operator Result<T>(T? value) => Create(value);
}
