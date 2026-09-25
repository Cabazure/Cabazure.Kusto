using System.Data;
using System.Runtime.CompilerServices;

namespace Cabazure.Kusto;

public abstract record StreamKustoQuery<T> : KustoScript, IKustoStreamQuery<T>
{
    public virtual async IAsyncEnumerable<T> ReadResults(
        IDataReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!reader.Read())
            {
                yield break;
            }

            yield return reader.ReadObject<T>(DataReaderExtensions.DefaultJsonOption);
        }
    }
}
