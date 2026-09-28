namespace Cabazure.Kusto.Ingest;

public interface IKustoIngesterFactory
{
    IKustoIngester<T> Create<T>(
        string? connectionName = null,
        string? databaseName = null,
        KustoIngestionMode? mode = null);
}
