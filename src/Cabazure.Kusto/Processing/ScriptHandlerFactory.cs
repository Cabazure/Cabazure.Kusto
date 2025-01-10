namespace Cabazure.Kusto.Processing;

public class ScriptHandlerFactory(
    IQueryIdProvider queryIdProvider,
    IKustoClientProvider clientProvider)
    : IScriptHandlerFactory
{
    public IScriptHandler Create(
        IKustoCommand command,
        string? connectionName = null,
        string? databaseName = null)
        => new SimpleCommandHandler(
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            command);

    public IScriptHandler<T> Create<T>(
        IKustoQuery<T> query,
        string? connectionName = null,
        string? databaseName = null)
        => new SimpleQueryHandler<T>(
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query);

    public IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken,
        string? connectionName = null,
        string? databaseName = null)
        => continuationToken != null
         ? new ExistingStoredQueryHandler<T>(
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query,
            maxItemCount,
            continuationToken)
         : new NewStoredQueryHandler<T>(
            queryIdProvider,
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            query,
            sessionId,
            maxItemCount);
}