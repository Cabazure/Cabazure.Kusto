using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cabazure.Kusto;

internal static class NullableStringResultMapping
{
    // Query APIs carry serializer options, so associate the row policy with that
    // same instance without changing JSON serialization or query interfaces.
    private static readonly ConditionalWeakTable<JsonSerializerOptions, Mapping> Mappings = new();

    public static bool IsEnabled(JsonSerializerOptions options)
        => Mappings.TryGetValue(options, out var mapping) && mapping.Enabled;

    public static void Configure(JsonSerializerOptions options, bool enabled)
    {
        if (options.IsReadOnly)
        {
            throw new InvalidOperationException("Result mapping must be configured before reading query results.");
        }

        Mappings.GetOrCreateValue(options).Enabled = enabled;
    }

    public static HashSet<string>? GetNullableProperties<T>(JsonSerializerOptions options)
    {
        if (!Mappings.TryGetValue(options, out var mapping) || !mapping.Enabled)
        {
            return null;
        }

        options.MakeReadOnly(populateMissingResolver: true);
        return mapping.Properties.GetOrAdd(typeof(T), type =>
        {
            var typeInfo = options.GetTypeInfo(type);
            return typeInfo.Kind == JsonTypeInfoKind.Object
                ? typeInfo.Properties
                    .Where(p => p.PropertyType == typeof(string)
                        && p.IsSetNullable
                        && (p.Set is not null || p.AssociatedParameter is not null))
                    .Select(p => p.Name)
                    .ToHashSet(options.PropertyNameCaseInsensitive
                        ? StringComparer.OrdinalIgnoreCase
                        : StringComparer.Ordinal)
                : [];
        });
    }

    private sealed class Mapping
    {
        public bool Enabled { get; set; }

        public ConcurrentDictionary<Type, HashSet<string>> Properties { get; } = new();
    }
}