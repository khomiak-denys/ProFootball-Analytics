using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Team.Dtos;

namespace ProFootball.Application.Team.Queries;

public sealed record TeamSearchQuery(
    string? Name,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<TeamListItemDto>>;
