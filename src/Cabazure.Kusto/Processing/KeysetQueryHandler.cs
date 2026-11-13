using System.Text.Json;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

internal sealed class KeysetQueryHandler<T>(
    IQueryIdProvider queryIdProvider,
    ICslQueryProvider queryProvider,
    IKustoQuery<IReadOnlyList<T>> query,
    KeysetPagination pagination,
    JsonSerializerOptions serializerOptions,
    string? connectionScope,
    string? databaseName)
    : IScriptHandler<PagedResult<T>>
{
    public async Task<PagedResult<T>?> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var queryText = query.GetQueryText();
        var parameters = query.GetCslParameters().ToDictionary(p => p.Key, p => p.Value);
        var columns = pagination.Order.ResolveColumns(serializerOptions);
        var fingerprint = KeysetContinuationToken.CreateFingerprint(
            queryIdProvider.CreateFingerprint(query.GetType(), queryText, parameters),
            columns,
            connectionScope,
            databaseName);
        var continuation = pagination.ContinuationToken is { } token
            ? KeysetContinuationToken.Parse(token, fingerprint, columns.Length)
            : null;

        if (pagination.ContinuationToken is not null && continuation is null)
        {
            return null;
        }

        var request = KeysetQueryBuilder.Create(
            queryText,
            parameters,
            columns,
            pagination.MaxItemCount,
            continuation?.Values);

        using var reader = await queryProvider.ExecuteQueryAsync(
            databaseName: null,
            request.Text,
            request.Properties,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var pageReader = new KeysetPageReader(reader, columns, pagination.MaxItemCount);
        var items = query.ReadResult(pageReader, serializerOptions);
        cancellationToken.ThrowIfCancellationRequested();

        if (items is null)
        {
            return null;
        }

        if (!pageReader.Completed || items.Count != pageReader.RowsRead)
        {
            throw new InvalidOperationException(
                "Keyset result mapping must consume the page and preserve one item per row in the original order.");
        }

        return new(
            items,
            pageReader.HasMore
                ? new KeysetContinuationToken(fingerprint, pageReader.LastValues).ToString()
                : null);
    }
}