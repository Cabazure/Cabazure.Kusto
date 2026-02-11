using System.Buffers;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto;

public static class DataReaderExtensions
{
    const int DotNetDecimalMaxPrecision = 28;
    const int DotNetDecimalMaxScale = 27;

    public static readonly JsonSerializerOptions DefaultJsonOption = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new DateOnlyJsonConverter(),
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static T[] ReadObjectsFromNextResult<T>(
        this IDataReader reader)
        => reader.NextResult()
         ? reader.ReadObjects<T>()
         : [];

    public static T[] ReadObjects<T>(
        this IDataReader reader,
        JsonSerializerOptions? options = null)
    {
        options ??= DefaultJsonOption;

        var buffer = new ArrayBufferWriter<byte>();
        using var doc = new Utf8JsonWriter(buffer);
        var results = new List<T>();
        while (reader.Read())
        {
            buffer.Clear();
            doc.Reset(buffer);

            doc.WriteStartObject();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                if (options.PropertyNamingPolicy is { } np)
                {
                    name = np.ConvertName(name);
                }

                var value = reader.GetDataTypeName(i) switch
                {
                    nameof(SByte) => reader.GetBoolean(i),
                    _ => reader.GetValue(i)
                };

                doc.WritePropertyName(name);

                if (value is JToken jtoken)
                {
                    doc.WriteRawValue(
                        jtoken.ToString(
                            Newtonsoft.Json.Formatting.None));
                }
                else if (value is DBNull)
                {
                    doc.WriteNullValue();
                }
                else if (value is SqlDecimal sd)
                {
                    doc.WriteNumberValue(sd.ToDecimal());
                }
                else
                {
                    JsonSerializer.Serialize(doc, value, options);
                }
            }

            doc.WriteEndObject();
            doc.Flush();

            results.Add(JsonSerializer.Deserialize<T>(buffer.WrittenSpan, options)!);
        }

        return [.. results];
    }

    private static decimal ToDecimal(this SqlDecimal sqlDecimal)
    {
        var integerDigits = sqlDecimal.Precision - sqlDecimal.Scale;
        if (integerDigits > DotNetDecimalMaxPrecision)
        {
            throw new OverflowException(
                $"Integer part ({integerDigits} digits) exceeds decimal capacity");
        }

        var maxAvailableScale = DotNetDecimalMaxPrecision - integerDigits;
        var targetScale = Math.Min(sqlDecimal.Scale, Math.Min(DotNetDecimalMaxScale, maxAvailableScale));
        var targetPrecision = Math.Min(sqlDecimal.Precision, integerDigits + targetScale);

        if (targetPrecision != sqlDecimal.Precision || targetScale != sqlDecimal.Scale)
        {
            var safeDecimal = SqlDecimal.ConvertToPrecScale(sqlDecimal, targetPrecision, targetScale);
            return (decimal)safeDecimal;
        }

        return (decimal)sqlDecimal;
    }

    public class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var strValue = reader.GetString()
                ?? throw new JsonException(
                    $"Unable to convert null to {nameof(DateOnly)}.");

            if (DateOnly.TryParse(strValue, CultureInfo.InvariantCulture, out var dateOnly))
            {
                return dateOnly;
            }

            if (DateTimeOffset.TryParse(strValue, CultureInfo.InvariantCulture, out var dateTimeOffset))
            {
                return DateOnly.FromDateTime(dateTimeOffset.DateTime);
            }

            throw new JsonException(
                $"Unable to convert \"{strValue}\" to {nameof(DateOnly)}.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateOnly value,
            JsonSerializerOptions options)
            => writer.WriteStringValue(
                value.ToString());
    }
}
