namespace ProFootball.Application.Auth.Dtos;

public sealed record PersistedSessionDto(
    int Version,
    string Login,
    string DisplayName,
    string Role,
    DateTime CreatedAtUtc)
{
    public const int CurrentVersion = 1;
}
