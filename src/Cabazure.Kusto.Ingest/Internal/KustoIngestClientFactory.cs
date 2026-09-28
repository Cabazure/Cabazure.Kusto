using Kusto.Data;
using Kusto.Ingest;

namespace Cabazure.Kusto.Ingest.Internal;

internal class KustoIngestClientFactory : IKustoIngestClientFactory
{
    public IKustoIngestClient Create(
        KustoIngestionMode mode,
        KustoConnectionStringBuilder connectionString)
        => mode switch
        {
            KustoIngestionMode.ManagedStreaming
                => KustoIngestFactory.CreateManagedStreamingIngestClient(
                    connectionString,
                    ingestPolicy: new ManagedStreamingIngestPolicy
                    {
                        ContinueWhenStreamingIngestionUnavailable = true,
                    }),
            KustoIngestionMode.Streaming
                => KustoIngestFactory.CreateStreamingIngestClient(
                    connectionString),
            KustoIngestionMode.Queued
                => KustoIngestFactory.CreateQueuedIngestClient(
                    connectionString),
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unsupported Kusto ingestion mode."),
        };
}
