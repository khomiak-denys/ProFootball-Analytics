namespace ProFootball.Application.Contracts.Queries;

public sealed record DashboardKpiDto(
    int Countries,
    int Leagues,
    int Teams,
    int Players,
    int Matches);
