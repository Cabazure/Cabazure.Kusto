using System.Globalization;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public class NewStoredQueryHandler<T>(
    IQueryIdProvider queryIdProvider,
    ICslAdminProvider adminProvider,
    IKustoQuery<IReadOnlyList<T>> query,
    string? sessionId,
    int maxItemCount,
    TimeSpan expiration,
    bool includeTotalCount = false)
    : IScriptHandler<PagedResult<T>>
{
    public async Task<PagedResult<T>?> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var queryId = queryIdProvider.Create(query.GetType(), sessionId);
        var header = $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {maxItemCount}, expiresAfter = {(long)expiration.TotalSeconds}s) <|";
        var footer = $"| serialize row_number = row_number()";
        var queryText = $"{header}\n{query.GetQueryText().Trim(' ', '\n', '\t', ';')}\n{footer}";

        IReadOnlyList<T>? result;
        using (var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                queryText,
                query.GetRequestProperties()))
        {
            result = query.ReadResult(reader);
        }

        if (result is not { } items)
        {
            return null;
        }

        if (items.Count < maxItemCount)
        {
            return new(items, null, includeTotalCount ? items.Count : null);
        }

        if (!includeTotalCount)
        {
            return new(items, $"{queryId};{items.Count}");
        }

        return await GetTotalCountAsync(queryId) switch
        {
            { } total when items.Count >= total => new(items, null, total),
            { } total => new(items, $"{queryId};{items.Count};{total}", total),
            _ => new(items, $"{queryId};{items.Count}"),
        };
    }

    private async Task<long?> GetTotalCountAsync(string queryId)
    {
        using var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                $".show stored_query_results ['{queryId}']",
                query.GetRequestProperties());

        if (!reader.Read())
        {
            return null;
        }

        var value = reader.GetValue(reader.GetOrdinal("RowCount"));
        return value is null or DBNull
            ? null
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }
}
