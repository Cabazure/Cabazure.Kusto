using System.Data;
using System.Text.Json;

namespace Cabazure.Kusto;

public interface IKustoQuery<out T> : IKustoScript
{
    T? ReadResult(IDataReader reader);

    T? ReadResult(
        IDataReader reader,
        JsonSerializerOptions serializerOptions)
        => ReadResult(reader);
}