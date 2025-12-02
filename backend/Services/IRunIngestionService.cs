using backend.Models;

namespace backend.Services;

public interface IRunIngestionService
{
    Task<RunIngestResponse> IngestAsync(IReadOnlyCollection<RunIngestRow> rows, string sourceName, CancellationToken cancellationToken = default);
    Task BackfillDimensionsFromStagingAsync(CancellationToken cancellationToken = default);
}
