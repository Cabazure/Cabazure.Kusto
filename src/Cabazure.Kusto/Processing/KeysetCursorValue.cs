using System.Data.SqlTypes;
using System.Globalization;

namespace Cabazure.Kusto.Processing;

internal sealed record KeysetCursorValue(string Type, string? Value)
{
    internal bool IsNull => Type == "null";

    public static KeysetCursorValue FromValue(object? value)
        => value switch
        {
            null or DBNull => new("null", null),
            SqlDecimal { IsNull: true } => new("null", null),
            string text => new("string", text),
            bool boolean => new("bool", boolean ? "true" : "false"),
            int number => new("int", number.ToString(CultureInfo.InvariantCulture)),
            long number => new("long", number.ToString(CultureInfo.InvariantCulture)),
            double number when double.IsFinite(number) => new("real", number.ToString("R", CultureInfo.InvariantCulture)),
            decimal number => new("decimal", number.ToString(CultureInfo.InvariantCulture)),
            SqlDecimal number => new("decimal", number.ToString()),
            DateTimeOffset timestamp => new("datetime", timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            DateTime timestamp => FromValue(new DateTimeOffset(
                timestamp.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                    : timestamp.ToUniversalTime())),
            TimeSpan duration => new("timespan", duration.ToString("c", CultureInfo.InvariantCulture)),
            _ => throw new NotSupportedException(
                $"The keyset cursor value type '{value.GetType().Name}' is not supported."),
        };

    public string GetCslValue()
        => Type switch
        {
            "null" when Value is null => "dynamic(null)",
            "string" when Value is not null => KustoScriptExtensions.GetCslValue(Value),
            "bool" when Value is "true" or "false" => Value,
            "int" => KustoScriptExtensions.GetCslValue(
                int.Parse(Value!, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)),
            "long" => KustoScriptExtensions.GetCslValue(
                long.Parse(Value!, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)),
            "real" => GetRealLiteral(Value!),
            "decimal" => GetDecimalLiteral(Value!),
            "datetime" => KustoScriptExtensions.GetCslValue(
                DateTimeOffset.ParseExact(Value!, "O", CultureInfo.InvariantCulture)),
            "timespan" => KustoScriptExtensions.GetCslValue(
                TimeSpan.ParseExact(Value!, "c", CultureInfo.InvariantCulture)),
            _ => throw new FormatException("Invalid keyset cursor value."),
        };

    private static string GetRealLiteral(string value)
    {
        var number = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        return double.IsFinite(number)
            ? KustoScriptExtensions.GetCslValue(number)
            : throw new FormatException("Non-finite keyset cursor values are not supported.");
    }

    private static string GetDecimalLiteral(string value)
    {
        var number = SqlDecimal.Parse(value);
        return !number.IsNull
            ? $"decimal({number})"
            : throw new FormatException("Null cursor values must use the null type.");
    }
}