using System.Data;
using System.Runtime.CompilerServices;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public class StreamQueryHandler<T>(
    ICslQueryProvider queryProvider,
    IKustoStreamQuery<T> query) : IStreamScriptHandler<T>
{
    public async IAsyncEnumerable<T> ExecuteAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = await queryProvider
            .ExecuteQueryAsync(
                databaseName: null,
                query.GetQueryText(),
                query.GetRequestProperties(),
                cancellationToken);

        try
        {
            await foreach (var item in query
                .ReadResults(reader, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            await DisposeAsync(reader);
        }
    }

    private static async ValueTask DisposeAsync(IDataReader reader)
    {
        if (reader is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
            return;
        }

        reader.Dispose();
    }
}
