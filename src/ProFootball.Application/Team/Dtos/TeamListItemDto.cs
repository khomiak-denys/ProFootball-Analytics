namespace ProFootball.Application.Team.Dtos;

public sealed record TeamListItemDto(
    int TeamApiId,
    string LongName,
    string? ShortName,
    int? TeamFifaApiId);
