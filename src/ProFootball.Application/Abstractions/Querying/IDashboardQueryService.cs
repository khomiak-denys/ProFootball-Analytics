using ProFootball.Application.Contracts.Queries;

namespace ProFootball.Application.Abstractions.Querying;

public interface IDashboardQueryService
{
    Task<DashboardKpiDto> GetKpisAsync(CancellationToken cancellationToken = default);
}
