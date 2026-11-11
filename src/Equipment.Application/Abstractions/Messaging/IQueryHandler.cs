namespace Equipment.Application.Abstractions.Messaging;

/// <summary>
/// Выполняет один сценарий чтения без изменения состояния
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
