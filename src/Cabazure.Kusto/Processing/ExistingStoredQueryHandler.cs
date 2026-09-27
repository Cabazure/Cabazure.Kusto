using Kusto.Data.Common;
using Kusto.Data.Exceptions;

namespace Cabazure.Kusto.Processing;

public class ExistingStoredQueryHandler<T>(
    ICslQueryProvider queryProvider,
    IKustoQuery<IReadOnlyList<T>> query,
    int maxItemCount,
    string continuationToken)
    : IScriptHandler<PagedResult<T>>
{
    public async Task<PagedResult<T>?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var split = continuationToken.Split(';');
        if (split.Length is not (2 or 3)
            || !long.TryParse(split[1], out var itemsReturned))
        {
            return null;
        }

        long? totalCount = null;
        if (split.Length == 3)
        {
            if (!long.TryParse(split[2], out var total))
            {
                return null;
            }

            totalCount = total;
        }

        var queryId = split[0].ToAlphaNumeric();
        var firstRowNum = itemsReturned + 1;
        var lastRowNum = itemsReturned + maxItemCount;
        var queryText = $"stored_query_result('{queryId}') | where row_number between({firstRowNum} .. {lastRowNum})";

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
                    CreateContinuationToken(queryId, itemsReturned + items.Count, items.Count, totalCount),
                    totalCount),
                _ => null,
            };
        }
        catch (SemanticException)
        {
            return null;
        }
    }

    private string? CreateContinuationToken(
        string queryId,
        long itemsReturned,
        int pageCount,
        long? totalCount)
        => totalCount switch
        {
            _ when pageCount < maxItemCount => null,
            { } total when itemsReturned >= total => null,
            { } total => $"{queryId};{itemsReturned};{total}",
            _ => $"{queryId};{itemsReturned}",
        };
}
