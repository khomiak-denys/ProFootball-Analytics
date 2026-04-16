namespace ProFootball.Application.Contracts.Queries;

public sealed record TeamListItemDto(
    int TeamApiId,
    string LongName,
    string? ShortName,
    int? TeamFifaApiId);
