using ProFootball.Application.Contracts.Importing;

namespace ProFootball.Application.Abstractions.Querying;

public interface IDataImportService
{
    Task<DataImportResult> ImportAsync(
        DataImportRequest request,
        CancellationToken cancellationToken = default);
}
