using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonElement = System.Text.Json.JsonElement;

namespace Cabazure.Kusto;

public sealed class NewtonsoftObjectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
        => objectType == typeof(object);

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
        => reader switch
        {
            JTokenReader { CurrentToken: { } token }
                => Convert(token),
            _ => null,
        };

    public override void WriteJson(
        JsonWriter writer,
        object? value,
        JsonSerializer serializer)
        => throw new NotSupportedException();

    private static JsonElement Convert(JToken token)
    {
        var utf8 = Encoding.UTF8.GetBytes(token.ToString());
        var jsonReader = new System.Text.Json.Utf8JsonReader(utf8);

        return JsonElement.ParseValue(ref jsonReader);
    }
}
