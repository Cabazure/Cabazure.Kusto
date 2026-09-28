using System.Text.Json;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public class NewStoredQueryHandler<T>(
    IQueryIdProvider queryIdProvider,
    ICslAdminProvider adminProvider,
    IKustoQuery<IReadOnlyList<T>> query,
    string? sessionId,
    int maxItemCount,
    TimeSpan expiration,
    bool includeTotalCount = false,
    JsonSerializerOptions? serializerOptions = null)
    : IScriptHandler<PagedResult<T>>
{
    private readonly JsonSerializerOptions serializerOptions
        = serializerOptions ?? DataReaderExtensions.DefaultJsonOption;

    public async Task<PagedResult<T>?> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var nonce = sessionId is null ? Guid.NewGuid().ToString("N") : null;
        var queryId = queryIdProvider.CreateQueryId(query, sessionId, nonce);
        var fingerprint = queryIdProvider.CreateFingerprint(query);
        var queryText = StoredQueryResultCommands.CreateSetOrReplaceCommand(
            query,
            queryId,
            fingerprint,
            maxItemCount,
            expiration);

        IReadOnlyList<T>? result;
        using (var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                queryText,
                query.GetRequestProperties()))
        {
            result = query.ReadResult(
                reader,
                serializerOptions);
        }

        if (result is not { } items)
        {
            return null;
        }

        if (items.Count < maxItemCount)
        {
            return new(items, null, includeTotalCount ? items.Count : null);
        }

        var totalCount = includeTotalCount
            ? await StoredQueryResultCommands.GetTotalCountAsync(adminProvider, query, queryId)
            : null;

        return new(
            items,
            StoredQueryContinuationToken
                .CreateNext(nonce, items.Count, items.Count, maxItemCount, totalCount)
                ?.ToString(),
            totalCount);
    }
}
