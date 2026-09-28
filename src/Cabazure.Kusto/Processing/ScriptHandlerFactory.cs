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
            query,
            GetOptions(connectionName).SerializerOptions);

    public IStreamScriptHandler<T> CreateStream<T>(
        IKustoStreamQuery<T> query,
        string? connectionName = null,
        string? databaseName = null)
        => new StreamQueryHandler<T>(
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            query,
            GetOptions(connectionName).SerializerOptions);

    public IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken,
        string? connectionName = null,
        string? databaseName = null,
        bool includeTotalCount = false)
    {
        CabazureKustoOptions options = GetOptions(connectionName);
        TimeSpan expiration = GetPagedResultExpiration(options);

        return continuationToken != null
         ? new ExistingStoredQueryHandler<T>(
            queryIdProvider,
            clientProvider.GetQueryClient(
                connectionName,
                databaseName),
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            query,
            sessionId,
            maxItemCount,
            expiration,
            continuationToken,
            options.SerializerOptions)
         : new NewStoredQueryHandler<T>(
            queryIdProvider,
            clientProvider.GetAdminClient(
                connectionName,
                databaseName),
            query,
            sessionId,
            maxItemCount,
            expiration,
            includeTotalCount,
            options.SerializerOptions);
    }

    private static TimeSpan GetPagedResultExpiration(
        CabazureKustoOptions options)
    {
        TimeSpan expiration = options.PagedResultExpiration;

        if (expiration < TimeSpan.FromSeconds(1)
            || expiration > CabazureKustoOptions.MaxPagedResultExpiration)
        {
            throw new InvalidOperationException(
                $"{nameof(CabazureKustoOptions.PagedResultExpiration)} must be between 1 second and 24 hours, but was {expiration}.");
        }

        return expiration;
    }

    private CabazureKustoOptions GetOptions(string? connectionName)
        => optionsMonitor.Get(connectionName)
            ?? new CabazureKustoOptions();
}
