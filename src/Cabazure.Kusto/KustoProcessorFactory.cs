using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto;

public class KustoProcessorFactory(
    IScriptHandlerFactory factory)
    : IKustoProcessorFactory
{
    public IKustoProcessor Create(
        string? connectionName = null,
        string? databaseName = null)
        => new KustoProcessor(
            factory,
            connectionName,
            databaseName);
}
