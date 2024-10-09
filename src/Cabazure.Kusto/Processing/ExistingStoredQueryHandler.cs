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
        if (split.Length != 2)
        {
            return null;
        }

        var queryId = split[0].ToAlphaNumeric();
        var itemsReturned = long.Parse(split[1]);
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
                { } items when items.Count < maxItemCount => new(items, null),
                { } items => new(items, $"{queryId};{itemsReturned + items.Count}"),
                _ => null,
            };
        }
        catch (SemanticException)
        {
            return null;
        }
    }
}
