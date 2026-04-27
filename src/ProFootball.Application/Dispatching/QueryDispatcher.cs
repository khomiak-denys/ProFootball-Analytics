using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Common.Errors;

namespace ProFootball.Application.Dispatching;

public sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        var result = await DispatchResultAsync<TQuery, TResult>(query, cancellationToken);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new InvalidOperationException(result.Error.Message);
    }

    public async Task<Result<TResult>> DispatchResultAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        try
        {
            return await handler.HandleAsync(query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (OutOfMemoryException)
        {
            throw;
        }
        catch (DbException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<TResult>.Failure(Error.Unexpected(exception.Message));
        }
    }
}
