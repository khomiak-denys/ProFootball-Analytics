using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Dtos;

namespace ProFootball.Application.Auth.Queries;

public sealed record GetSessionStateQuery : IQuery<SessionStateDto>;
