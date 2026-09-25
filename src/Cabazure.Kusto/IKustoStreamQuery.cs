using System.Data;

namespace Cabazure.Kusto;

public interface IKustoStreamQuery<out T> : IKustoScript
{
    IAsyncEnumerable<T> ReadResults(
        IDataReader reader,
        CancellationToken cancellationToken);
}
