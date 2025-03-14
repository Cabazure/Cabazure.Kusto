using Kusto.Data.Common;

namespace Cabazure.Kusto;

public interface IKustoClientProvider
{
    ICslAdminProvider GetAdminClient(
        string? connectionName = null,
        string? databaseName = null);

    ICslQueryProvider GetQueryClient(
        string? connectionName = null,
        string? databaseName = null);
}