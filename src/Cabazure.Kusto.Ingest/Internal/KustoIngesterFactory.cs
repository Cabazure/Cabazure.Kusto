using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Ingest.Internal;

internal class KustoIngesterFactory(
    IServiceProvider serviceProvider,
    IKustoIngestClientProvider clientProvider,
    IKustoConnectionStringProvider connectionStringProvider,
    IOptionsMonitor<CabazureKustoOptions> optionsMonitor)
    : IKustoIngesterFactory
{
    public IKustoIngester<T> Create<T>(
        string? connectionName = null,
        string? databaseName = null,
        KustoIngestionMode? mode = null)
    {
        KustoIngestion<T> ingestion = serviceProvider
            .GetRequiredService<KustoIngestion<T>>();
        string? effectiveConnectionName
            = connectionName ?? ingestion.ConnectionName;
        string effectiveDatabaseName = connectionStringProvider
            .GetConnectionString(
                effectiveConnectionName,
                databaseName)
            .InitialCatalog
            ?? throw new InvalidOperationException(
                $"Missing database configuration for kusto connection `{effectiveConnectionName}`.");

        return new KustoIngester<T>(
            clientProvider,
            ingestion,
            optionsMonitor
                .Get(effectiveConnectionName)
                .SerializerOptions,
            effectiveConnectionName,
            effectiveDatabaseName,
            mode ?? ingestion.Mode);
    }
}
