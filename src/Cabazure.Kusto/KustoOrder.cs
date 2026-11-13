using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cabazure.Kusto;

public class KustoOrder
{
    internal KustoOrder(ImmutableArray<Key> keys)
    {
        Keys = keys;
    }

    internal ImmutableArray<Key> Keys { get; }

    public static KustoOrder Ascending(string columnName)
        => new([CreateKey(columnName, descending: false)]);

    public static KustoOrder Descending(string columnName)
        => new([CreateKey(columnName, descending: true)]);

    public KustoOrder ThenAscending(string columnName)
        => new(Keys.Add(CreateKey(columnName, descending: false)));

    public KustoOrder ThenDescending(string columnName)
        => new(Keys.Add(CreateKey(columnName, descending: true)));

    internal ImmutableArray<Column> ResolveColumns(JsonSerializerOptions options)
    {
        var columns = ImmutableArray.CreateBuilder<Column>(Keys.Length);
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in Keys)
        {
            var name = key.ColumnName
                ?? key.Property!.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? options.PropertyNamingPolicy?.ConvertName(key.Property!.Name)
                ?? key.Property!.Name;

            if (!names.Add(name))
            {
                throw new ArgumentException($"The ordering column '{name}' is specified more than once.");
            }

            columns.Add(new(name, key.Descending));
        }

        return columns.MoveToImmutable();
    }

    internal static Key CreateKey(string columnName, bool descending)
        => new(columnName, null, descending);

    internal sealed record Key(string? ColumnName, PropertyInfo? Property, bool Descending);

    internal sealed record Column(string Name, bool Descending);
}