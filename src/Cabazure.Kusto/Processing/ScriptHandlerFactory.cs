using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Processing;

public class ScriptHandlerFactory(
    IQueryIdProvider queryIdProvider,
    IKustoClientProvider clientProvider,
    IOptionsMonitor<CabazureKustoOptions> optionsMonitor)
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

    public IStreamScriptHandler<T> CreateStream<T>(
        IKustoStreamQuery<T> query,
        string? connectionName = null,
        string? databaseName = null)
        => new StreamQueryHandler<T>(
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
        string? databaseName = null,
        bool includeTotalCount = false)
        => continuationToken != null
         ? new ExistingStoredQueryHandler<T>(
            queryIdProvider,
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query,
            sessionId,
            maxItemCount,
            continuationToken)
         : new NewStoredQueryHandler<T>(
            queryIdProvider,
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            query,
            sessionId,
            maxItemCount,
            GetPagedResultExpiration(connectionName),
            includeTotalCount);

    private TimeSpan GetPagedResultExpiration(string? connectionName)
    {
        var expiration = optionsMonitor.Get(connectionName)?.PagedResultExpiration
            ?? CabazureKustoOptions.DefaultPagedResultExpiration;

        if (expiration < TimeSpan.FromSeconds(1)
            || expiration > CabazureKustoOptions.MaxPagedResultExpiration)
        {
            throw new InvalidOperationException(
                $"{nameof(CabazureKustoOptions.PagedResultExpiration)} must be between 1 second and 24 hours, but was {expiration}.");
        }

        return expiration;
    }
}
