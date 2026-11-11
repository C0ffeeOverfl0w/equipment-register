namespace Equipment.Application.Abstractions.Messaging;

/// <summary>
/// Обозначает запрос MediatR без изменения состояния
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
