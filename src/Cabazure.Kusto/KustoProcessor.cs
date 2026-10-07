using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto;

public class KustoProcessor(
    IScriptHandlerFactory factory,
    string? connectionName,
    string? databaseName)
    : IKustoProcessor
{
    public string? ConnectionName { get; }
        = connectionName;

    public string? DatabaseName { get; }
        = databaseName;

    public async Task ExecuteAsync(
        IKustoCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await factory
                .Create(
                    command,
                    ConnectionName,
                    DatabaseName)
                .ExecuteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.IsCanceledBy(cancellationToken))
        {
            throw ex.ToOperationCanceled(cancellationToken);
        }
    }

    public async Task<T?> ExecuteAsync<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await factory
                .Create(
                    query,
                    ConnectionName,
                    DatabaseName)
                .ExecuteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.IsCanceledBy(cancellationToken))
        {
            throw ex.ToOperationCanceled(cancellationToken);
        }
    }

    public Task<PagedResult<T>?> ExecuteAsync<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int? maxItemCount,
        string? continuationToken,
        CancellationToken cancellationToken)
        => ExecuteAsync(
            query,
            sessionId,
            maxItemCount,
            continuationToken,
            includeTotalCount: false,
            cancellationToken);

    public async Task<PagedResult<T>?> ExecuteAsync<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int? maxItemCount,
        string? continuationToken,
        bool includeTotalCount,
        CancellationToken cancellationToken)
    {
        if (maxItemCount is { } count)
        {
            try
            {
                return await factory
                    .Create(
                        query,
                        sessionId,
                        count,
                        continuationToken,
                        ConnectionName,
                        DatabaseName,
                        includeTotalCount)
                    .ExecuteAsync(cancellationToken);
            }
            catch (Exception ex) when (ex.IsCanceledBy(cancellationToken))
            {
                throw ex.ToOperationCanceled(cancellationToken);
            }
        }

        IReadOnlyList<T> items = await ExecuteAsync(query, cancellationToken) ?? [];
        return new PagedResult<T>(
            Items: items,
            ContinuationToken: null,
            TotalCount: includeTotalCount ? items.Count : null);
    }

    public IAsyncEnumerable<T> ExecuteAsync<T>(
        IKustoStreamQuery<T> query,
        CancellationToken cancellationToken)
        => factory
            .CreateStream(
                query,
                ConnectionName,
                DatabaseName)
            .ExecuteAsync(cancellationToken)
            .WithCancellationTranslation(cancellationToken);
}
