using System.Data;
using System.Text.Json;

namespace Cabazure.Kusto;

public interface IKustoStreamQuery<out T> : IKustoScript
{
    IAsyncEnumerable<T> ReadResults(
        IDataReader reader,
        CancellationToken cancellationToken);

    IAsyncEnumerable<T> ReadResults(
        IDataReader reader,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken)
        => ReadResults(reader, cancellationToken);
}
