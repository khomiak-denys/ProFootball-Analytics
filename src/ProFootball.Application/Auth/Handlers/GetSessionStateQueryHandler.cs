using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Dtos;
using ProFootball.Application.Auth.Queries;
using ProFootball.Application.Common;

namespace ProFootball.Application.Auth.Handlers;

public sealed class GetSessionStateQueryHandler(IUserSessionStore sessionStore) : IQueryHandler<GetSessionStateQuery, SessionStateDto>
{
    public Task<Result<SessionStateDto>> HandleAsync(GetSessionStateQuery _, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Result<SessionStateDto>.Success(sessionStore.CurrentState));
    }
}
