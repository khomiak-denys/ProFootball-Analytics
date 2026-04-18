namespace ProFootball.Application.Team.Dtos;

public sealed record TeamDetailsDto(
    TeamListItemDto Team,
    IReadOnlyList<TeamAttributeDto> Attributes);
