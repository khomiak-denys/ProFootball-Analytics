namespace ProFootball.Application.Auth.Dtos;

public sealed record SessionStateDto(
    bool IsAuthenticated,
    string? Login,
    string? DisplayName,
    string? Role);
