using System.Collections.Concurrent;
using Kusto.Ingest;

namespace Cabazure.Kusto.Ingest.Internal;

internal class KustoIngestClientProvider(
    IKustoConnectionStringProvider connectionStringProvider,
    IKustoIngestClientFactory clientFactory)
    : IKustoIngestClientProvider, IDisposable
{
    private sealed record ClientKey(
        string? ConnectionName,
        KustoIngestionMode Mode);

    private readonly ConcurrentDictionary<
        ClientKey,
        Lazy<IKustoIngestClient>> clients
        = new();

    public IKustoIngestClient GetClient(
        string? connectionName,
        KustoIngestionMode mode)
        => clients.GetOrAdd(
            new(connectionName, mode),
            key => new(
                () => CreateClient(key),
                LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;

    private IKustoIngestClient CreateClient(ClientKey key)
        => clientFactory.Create(
            key.Mode,
            connectionStringProvider.GetConnectionString(
                key.ConnectionName));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (Lazy<IKustoIngestClient> client in clients.Values)
        {
            if (client.IsValueCreated)
            {
                client.Value.Dispose();
            }
        }
    }
}
