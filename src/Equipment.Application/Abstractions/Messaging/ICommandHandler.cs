namespace Equipment.Application.Abstractions.Messaging;

/// <summary>
/// Обрабатывает команду через стандартный контракт MediatR
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
