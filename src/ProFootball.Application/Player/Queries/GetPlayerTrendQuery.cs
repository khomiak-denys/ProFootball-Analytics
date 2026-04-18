using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Player.Dtos;

namespace ProFootball.Application.Player.Queries;

public sealed record GetPlayerTrendQuery(int PlayerApiId) : IQuery<IReadOnlyList<PlayerTrendPointDto>>;
