using System.Data;
using System.Text.Json;

namespace Cabazure.Kusto;

public abstract record KustoQuery<T> : KustoScript, IKustoQuery<T[]>
{
    public virtual T[]? ReadResult(
        IDataReader reader,
        JsonSerializerOptions serializerOptions)
        => reader.ReadObjects<T>(serializerOptions);
}
