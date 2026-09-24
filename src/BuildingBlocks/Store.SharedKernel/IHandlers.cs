namespace Store.SharedKernel;

/// <summary>Handles a use case that changes state (CQRS "command").</summary>
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Handles a use case that only reads state (CQRS "query").</summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Handles a use case that changes state and has no response payload.</summary>
public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
