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
        string effectiveDatabaseName = connectionStringProvider
            .GetConnectionString(
                connectionName,
                databaseName)
            .InitialCatalog
            ?? throw new InvalidOperationException(
                $"Missing database configuration for kusto connection `{connectionName}`.");

        return new KustoIngester<T>(
            clientProvider,
            ingestion,
            optionsMonitor
                .Get(connectionName)
                .SerializerOptions,
            connectionName,
            effectiveDatabaseName,
            mode ?? ingestion.Mode);
    }
}
