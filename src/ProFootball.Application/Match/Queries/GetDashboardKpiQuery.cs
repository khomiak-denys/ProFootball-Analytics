using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Match.Dtos;

namespace ProFootball.Application.Match.Queries;

public sealed record GetDashboardKpiQuery : IQuery<DashboardKpiDto>;
