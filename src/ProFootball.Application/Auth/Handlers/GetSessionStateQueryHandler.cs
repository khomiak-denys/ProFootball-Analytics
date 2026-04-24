using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Dtos;
using ProFootball.Application.Auth.Queries;

namespace ProFootball.Application.Auth.Handlers;

public sealed class GetSessionStateQueryHandler(IUserSessionStore sessionStore) : IQueryHandler<GetSessionStateQuery, SessionStateDto>
{
    public Task<SessionStateDto> HandleAsync(GetSessionStateQuery _, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(sessionStore.CurrentState);
    }
}
