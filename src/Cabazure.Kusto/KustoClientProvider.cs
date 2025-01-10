using System.Collections.Concurrent;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto;

public class KustoClientProvider(
    IOptionsMonitor<CabazureKustoOptions> monitor)
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
            GetConnectionString(clientKey));

    private ICslAdminProvider CreateAdminClient(ClientKey clientKey)
        => KustoClientFactory.CreateCslAdminProvider(
            GetConnectionString(clientKey));

    private KustoConnectionStringBuilder GetConnectionString(
        ClientKey clientKey)
        => monitor.Get(clientKey.ConnectionName) switch
        {
            { HostAddress: { } host, DatabaseName: { } db, Credential: { } cred }
                => new KustoConnectionStringBuilder(host.AbsoluteUri, clientKey.DatabaseName ?? db)
                    .WithAadAzureTokenCredentialsAuthentication(cred),
            _ => throw new InvalidOperationException(
                $"Missing configuration for kusto connection `{clientKey.ConnectionName}`"),
        };

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
