using System.Data;

namespace Cabazure.Kusto;

public abstract record KustoQuery<T> : KustoScript, IKustoQuery<T[]>
{
    public virtual T[]? ReadResult(IDataReader reader)
        => reader.ReadObjects<T>();
}
