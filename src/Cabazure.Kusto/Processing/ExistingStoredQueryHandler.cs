using Kusto.Data.Common;
using Kusto.Data.Exceptions;

namespace Cabazure.Kusto.Processing;

public class ExistingStoredQueryHandler<T>(
    IQueryIdProvider queryIdProvider,
    ICslQueryProvider queryProvider,
    IKustoQuery<IReadOnlyList<T>> query,
    string? sessionId,
    int maxItemCount,
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
        var queryText = StoredQueryResultCommands.CreatePageQuery(
            queryId,
            fingerprint,
            token.ItemsReturned,
            maxItemCount);

        try
        {
            using var reader = await queryProvider
                .ExecuteQueryAsync(
                    databaseName: null,
                    queryText,
                    query.GetRequestProperties(),
                    cancellationToken);

            return query.ReadResult(reader) switch
            {
                { } items => new(
                    items,
                    StoredQueryContinuationToken
                        .CreateNext(
                            token.Nonce,
                            token.ItemsReturned + items.Count,
                            items.Count,
                            maxItemCount,
                            token.TotalCount)
                        ?.ToString(),
                    token.TotalCount),
                _ => null,
            };
        }
        catch (SemanticException)
        {
            return null;
        }
    }
}
