namespace Cabazure.Kusto.Processing;

public class ScriptHandlerFactory(
    IQueryIdProvider queryIdProvider,
    IKustoClientProvider clientProvider)
    : IScriptHandlerFactory
{
    public IScriptHandler Create(
        IKustoCommand command)
        => new SimpleCommandHandler(
            clientProvider.GetAdminClient(),
            command);

    public IScriptHandler<T> Create<T>(
        IKustoQuery<T> query)
        => new SimpleQueryHandler<T>(
            clientProvider.GetQueryClient(),
            query);

    public IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken)
        => continuationToken != null
         ? new ExistingStoredQueryHandler<T>(
            clientProvider.GetQueryClient(),
            query,
            maxItemCount,
            continuationToken)
         : new NewStoredQueryHandler<T>(
            queryIdProvider,
            clientProvider.GetAdminClient(),
            query,
            sessionId,
            maxItemCount);
}