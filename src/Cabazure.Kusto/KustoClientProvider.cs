using System.Collections.Concurrent;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;

namespace Cabazure.Kusto;

public class KustoClientProvider(
    IKustoConnectionStringProvider connectionStringProvider)
    : IDisposable, IKustoClientProvider
{
    private record ClientKey(string? ConnectionName, string? DatabaseName);
    private readonly ConcurrentDictionary<ClientKey, ICslQueryProvider> queryClients = new();
    private readonly ConcurrentDictionary<ClientKey, ICslAdminProvider> adminClients = new();

    public ICslQueryProvider GetQueryClient(
        string? connectionName = null,
        string? databaseName = null)
        => queryClients.GetOrAdd(
            new(connectionName, databaseName),
            CreateQueryClient);

    public ICslAdminProvider GetAdminClient(
        string? connectionName = null,
        string? databaseName = null)
        => adminClients.GetOrAdd(
            new(connectionName, databaseName),
            CreateAdminClient);

    private ICslQueryProvider CreateQueryClient(ClientKey clientKey)
        => KustoClientFactory.CreateCslQueryProvider(
            connectionStringProvider.GetConnectionString(
                clientKey.ConnectionName,
                clientKey.DatabaseName));

    private ICslAdminProvider CreateAdminClient(ClientKey clientKey)
        => KustoClientFactory.CreateCslAdminProvider(
            connectionStringProvider.GetConnectionString(
                clientKey.ConnectionName,
                clientKey.DatabaseName));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var adminClient in adminClients.Values)
        {
            adminClient.Dispose();
        }

        foreach (var queryClient in queryClients.Values)
        {
            queryClient.Dispose();
        }
    }
}
