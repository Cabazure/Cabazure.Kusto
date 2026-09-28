using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cabazure.Kusto;

public abstract record StreamKustoQuery<T> : KustoScript, IKustoStreamQuery<T>
{
    public virtual IAsyncEnumerable<T> ReadResults(
        IDataReader reader,
        CancellationToken cancellationToken = default)
        => ReadResults(
            reader,
            DataReaderExtensions.DefaultJsonOption,
            cancellationToken);

    public virtual async IAsyncEnumerable<T> ReadResults(
        IDataReader reader,
        JsonSerializerOptions serializerOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!reader.Read())
            {
                yield break;
            }

            yield return reader.ReadObject<T>(serializerOptions);
        }
    }
}
