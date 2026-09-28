using Kusto.Data;

namespace Cabazure.Kusto;

public interface IKustoConnectionStringProvider
{
    KustoConnectionStringBuilder GetConnectionString(
        string? connectionName = null,
        string? databaseName = null);
}
