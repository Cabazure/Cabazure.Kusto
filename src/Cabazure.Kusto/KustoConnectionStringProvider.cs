using Kusto.Data;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto;

public class KustoConnectionStringProvider(
    IOptionsMonitor<CabazureKustoOptions> monitor)
    : IKustoConnectionStringProvider
{
    public KustoConnectionStringBuilder GetConnectionString(
        string? connectionName = null,
        string? databaseName = null)
    {
        CabazureKustoOptions options = monitor.Get(connectionName);

        KustoConnectionStringBuilder builder = options switch
        {
            { HostAddress: { } host }
                => new KustoConnectionStringBuilder(host.AbsoluteUri),
            { ConnectionString: { } cs }
                => new KustoConnectionStringBuilder(cs),
            _ => throw new InvalidOperationException(
                $"Missing configuration for kusto connection `{connectionName}`"),
        };

        if ((databaseName ?? options.DatabaseName) is { } database)
        {
            builder.InitialCatalog = database;
        }

        return options.Credential is { } credential
            ? builder.WithAadAzureTokenCredentialsAuthentication(credential)
            : builder;
    }
}
