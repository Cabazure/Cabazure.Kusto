using System.Data.SqlTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto;

public sealed class NewtonsoftDecimalConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
        => objectType == typeof(decimal);

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
        => reader switch
        {
            JTokenReader { CurrentToken: { } token }
                => token.Value<decimal>(nameof(SqlDecimal.Value)),
            _ => null,
        };

    public override void WriteJson(
        JsonWriter writer,
        object? value,
        JsonSerializer serializer)
        => throw new NotSupportedException();
}
