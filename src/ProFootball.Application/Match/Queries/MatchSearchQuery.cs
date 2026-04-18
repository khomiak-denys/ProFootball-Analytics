using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Match.Dtos;

namespace ProFootball.Application.Match.Queries;

public sealed record MatchSearchQuery(
    int? LeagueId,
    string? Season,
    int? TeamApiId,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<MatchListItemDto>>;
