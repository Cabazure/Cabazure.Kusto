using System.Text.Json;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public class SimpleQueryHandler<T>(
    ICslQueryProvider queryProvider,
    IKustoQuery<T> query,
    JsonSerializerOptions? serializerOptions = null) : IScriptHandler<T>
{
    private readonly JsonSerializerOptions serializerOptions
        = serializerOptions ?? DataReaderExtensions.DefaultJsonOption;

    public async Task<T?> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        using var reader = await queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetRequestProperties(),
                cancellationToken);

        return query.ReadResult(
            reader,
            serializerOptions);
    }
}
