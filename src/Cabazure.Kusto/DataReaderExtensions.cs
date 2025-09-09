using System.Data;
using System.Data.SqlTypes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto;

public static class DataReaderExtensions
{
    private const int MxDecimalPrecision = 28;
    private const int MxDecimalScale = 27;
    private static readonly JsonSerializerOptions JsonOption = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static T[] ReadObjectsFromNextResult<T>(
        this IDataReader reader)
        => reader.NextResult()
         ? reader.ReadObjects<T>()
         : [];

    public static T[] ReadObjects<T>(
        this IDataReader reader)
    {
        var results = new List<T>();
        while (reader.Read())
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                dict[reader.GetName(i)] = reader.GetValue(i) switch
                {
                    DBNull => null,
                    JToken jToken => jToken.ToJsonElement(),
                    SqlDecimal sd => sd.ToDecimal(),
                    var value => value,
                };
            }

            string json = JsonSerializer.Serialize(dict, JsonOption);

            results.Add(JsonSerializer.Deserialize<T>(json, JsonOption)!);
        }

        return [.. results];
    }

    private static JsonElement ToJsonElement(this JToken token)
    {
        var utf8 = Encoding.UTF8.GetBytes(token.ToString());
        var jsonReader = new Utf8JsonReader(utf8);

        return JsonElement.ParseValue(ref jsonReader);
    }

    private static decimal? ToDecimal(this SqlDecimal sd)
    {
        if (sd.Precision > MxDecimalPrecision)
        {
            sd = SqlDecimal.ConvertToPrecScale(sd, MxDecimalPrecision, MxDecimalScale);
        }

        return sd.Value;
    }
}
