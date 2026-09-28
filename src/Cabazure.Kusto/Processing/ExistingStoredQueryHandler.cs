using Kusto.Data.Common;
using Kusto.Data.Exceptions;

namespace Cabazure.Kusto.Processing;

public class ExistingStoredQueryHandler<T>(
    IQueryIdProvider queryIdProvider,
    ICslQueryProvider queryProvider,
    ICslAdminProvider adminProvider,
    IKustoQuery<IReadOnlyList<T>> query,
    string? sessionId,
    int maxItemCount,
    TimeSpan expiration,
    string continuationToken)
    : IScriptHandler<PagedResult<T>>
{
    public async Task<PagedResult<T>?> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (StoredQueryContinuationToken.Parse(continuationToken) is not { } token)
        {
            return null;
        }

        var queryId = queryIdProvider.CreateQueryId(query, sessionId, token.Nonce);
        var fingerprint = queryIdProvider.CreateFingerprint(query);
        var totalCount = token.TotalCount;

        var items = await ReadPageAsync(queryId, fingerprint, token.ItemsReturned, cancellationToken);
        if (items is null
            || items.Count == 0 && !await HasMatchingRowsAsync(queryId, fingerprint, cancellationToken))
        {
            // The stored result has expired or been replaced by another query, so re-create it.
            await RecreateAsync(queryId, fingerprint);
            if (totalCount is not null)
            {
                totalCount = await StoredQueryResultCommands.GetTotalCountAsync(adminProvider, query, queryId);
            }

            items = await ReadPageAsync(queryId, fingerprint, token.ItemsReturned, cancellationToken);
        }

        return items switch
        {
            { } page => new(
                page,
                StoredQueryContinuationToken
                    .CreateNext(
                        token.Nonce,
                        token.ItemsReturned + page.Count,
                        page.Count,
                        maxItemCount,
                        totalCount)
                    ?.ToString(),
                totalCount),
            _ => null,
        };
    }

    private async Task<IReadOnlyList<T>?> ReadPageAsync(
        string queryId,
        string fingerprint,
        long itemsReturned,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reader = await queryProvider
                .ExecuteQueryAsync(
                    databaseName: null,
                    StoredQueryResultCommands.CreatePageQuery(
                        queryId,
                        fingerprint,
                        itemsReturned,
                        maxItemCount),
                    query.GetRequestProperties(),
                    cancellationToken);

            return query.ReadResult(reader);
        }
        catch (SemanticException)
        {
            return null;
        }
    }

    private async Task RecreateAsync(
        string queryId,
        string fingerprint)
    {
        using var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                StoredQueryResultCommands.CreateSetOrReplaceCommand(
                    query,
                    queryId,
                    fingerprint,
                    previewCount: maxItemCount,
                    expiration),
                query.GetRequestProperties());
    }

    private async Task<bool> HasMatchingRowsAsync(
        string queryId,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reader = await queryProvider
                .ExecuteQueryAsync(
                    databaseName: null,
                    StoredQueryResultCommands.CreateFingerprintProbeQuery(queryId, fingerprint),
                    query.GetRequestProperties(),
                    cancellationToken);

            return reader.Read();
        }
        catch (SemanticException)
        {
            return false;
        }
    }
}
