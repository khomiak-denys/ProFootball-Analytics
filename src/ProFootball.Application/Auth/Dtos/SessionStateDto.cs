namespace ProFootball.Application.Auth.Dtos;

public sealed record SessionStateDto(bool IsAuthenticated, string? CurrentUser);
