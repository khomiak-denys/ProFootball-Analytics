namespace ProFootball.Application.Country.Dtos;

public sealed record CountryLeagueListItemDto(string Name, int LeagueCount)
{
    public override string ToString() => Name;
}
