using Kusto.Data;
using Kusto.Ingest;

namespace Cabazure.Kusto.Ingest.Internal;

internal interface IKustoIngestClientFactory
{
    IKustoIngestClient Create(
        KustoIngestionMode mode,
        KustoConnectionStringBuilder connectionString);
}
