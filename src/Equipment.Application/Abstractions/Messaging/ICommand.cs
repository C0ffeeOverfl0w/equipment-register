namespace Equipment.Application.Abstractions.Messaging;

/// <summary>
/// Обозначает команду MediatR с типизированным результатом
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
