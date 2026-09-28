namespace Cabazure.Kusto.Ingest;

public interface IKustoIngester<T>
{
    Task<KustoIngestionResult> IngestAsync(
        IEnumerable<T> items,
        CancellationToken cancellationToken = default);

    Task<KustoIngestionResult> IngestAsync(
        IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default);
}
