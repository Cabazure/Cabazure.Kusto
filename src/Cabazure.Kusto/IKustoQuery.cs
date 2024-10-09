using System.Data;

namespace Cabazure.Kusto;

public interface IKustoQuery<out T> : IKustoScript
{
    T? ReadResult(IDataReader reader);
}