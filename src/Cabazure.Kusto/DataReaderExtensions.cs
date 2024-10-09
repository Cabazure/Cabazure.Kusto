using System.Data;
using Kusto.Cloud.Platform.Data;
using Newtonsoft.Json;

namespace Cabazure.Kusto;

public static class DataReaderExtensions
{
    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault(new()
    {
        Converters = { new NewtonsoftObjectConverter() },
    });

    public static T[] ReadObjects<T>(
        this IDataReader reader)
        => reader
            .ToJObjects()
            .Select(o => o.ToObject<T>(Serializer))
            .OfType<T>()
            .ToArray();

    public static T[] ReadObjectsFromNextResult<T>(
        this IDataReader reader)
        => reader.NextResult()
         ? reader.ReadObjects<T>()
         : Array.Empty<T>();
}
