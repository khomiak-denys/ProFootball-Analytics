namespace ProFootball.Application.Match.Dtos;

public sealed record DashboardKpiDto(
    int Countries,
    int Leagues,
    int Teams,
    int Players,
    int Matches);
