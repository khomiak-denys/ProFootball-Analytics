namespace ProFootball.Application.League.Dtos;

public sealed record LeagueDto(int Id, string Name, string CountryName)
{
    public override string ToString() => Name;
}
