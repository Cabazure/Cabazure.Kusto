namespace Cabazure.Kusto.Processing;

public interface IScriptHandlerFactory
{
    IScriptHandler Create(
        IKustoCommand command,
        string? connectionName = null,
        string? databaseName = null);

    IScriptHandler<T> Create<T>(
        IKustoQuery<T> query,
        string? connectionName = null,
        string? databaseName = null);

    IStreamScriptHandler<T> CreateStream<T>(
        IKustoStreamQuery<T> query,
        string? connectionName = null,
        string? databaseName = null);

    IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken,
        string? connectionName = null,
        string? databaseName = null);
}
