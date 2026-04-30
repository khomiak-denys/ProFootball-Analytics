using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Player.Dtos;

namespace ProFootball.Application.Player.Queries;

public sealed record GetTopRatingDeltaPlayersQuery(
    int Limit = 10,
    int MinimumSamples = 1) : IQuery<IReadOnlyList<PlayerRatingDeltaDto>>;
