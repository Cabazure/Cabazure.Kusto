namespace Cabazure.Kusto;

public interface IKustoProcessorFactory
{
    IKustoProcessor Create(
        string? connectionName = null,
        string? databaseName = null);
}