using Microsoft.Extensions.DependencyInjection;
using ProFootball.Application.Abstractions.Cqrs;

namespace ProFootball.Application.Dispatching;

public sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return handler.HandleAsync(query, cancellationToken);
    }
}
