using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public class NewStoredQueryHandler<T>(
    IQueryIdProvider queryIdProvider,
    ICslAdminProvider adminProvider,
    IKustoQuery<IReadOnlyList<T>> query,
    string? sessionId,
    int maxItemCount)
    : IScriptHandler<PagedResult<T>>
{
    public async Task<PagedResult<T>?> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var queryId = queryIdProvider.Create(query.GetType(), sessionId);
        var header = $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {maxItemCount}, expiresAfter = 1h) <|";
        var footer = $"| serialize row_number = row_number()";
        var queryText = $"{header}\n{query.GetQueryText().Trim(' ', '\n', '\t', ';')}\n{footer}";

        using var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                queryText,
                query.GetRequestProperties());

        return query.ReadResult(reader) switch
        {
            { } items when items.Count < maxItemCount => new(items, null),
            { } items => new(items, $"{queryId};{items.Count}"),
            _ => null,
        };
    }
}
