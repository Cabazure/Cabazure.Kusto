using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public class ScriptHandlerFactory(
    IQueryIdProvider queryIdProvider,
    ICslQueryProvider queryProvider,
    ICslAdminProvider adminProvider) 
    : IScriptHandlerFactory
{
    public IScriptHandler Create(
        IKustoCommand command)
        => new SimpleCommandHandler(
            adminProvider,
            command);

    public IScriptHandler<T> Create<T>(
        IKustoQuery<T> query)
        => new SimpleQueryHandler<T>(
            queryProvider,
            query);

    public IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken)
        => continuationToken != null
         ? new ExistingStoredQueryHandler<T>(
            queryProvider,
            query,
            maxItemCount,
            continuationToken)
         : new NewStoredQueryHandler<T>(
            queryIdProvider,
            adminProvider,
            query,
            sessionId,
            maxItemCount);
}