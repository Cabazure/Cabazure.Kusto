using Kusto.Ingest;

namespace Cabazure.Kusto.Ingest.Internal;

internal interface IKustoIngestClientProvider
{
    IKustoIngestClient GetClient(
        string? connectionName,
        KustoIngestionMode mode);
}
