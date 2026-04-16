namespace ProFootball.Application.Contracts.Queries;

public sealed record TeamDetailsDto(
    TeamListItemDto Team,
    IReadOnlyList<TeamAttributeDto> Attributes);
