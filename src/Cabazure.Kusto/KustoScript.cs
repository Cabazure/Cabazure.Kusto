using System.Reflection;

namespace Cabazure.Kusto;

public abstract record KustoScript
{
    private readonly Type type;

    protected KustoScript()
    {
        type = GetType();
    }

    public string GetQueryText()
    {
        var resourcePath = $"{type.FullName}.kusto";
        using var stream = type.Assembly.GetManifestResourceStream(resourcePath)
            ?? throw new FileNotFoundException("Could not load embedded resource.", resourcePath);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    public IDictionary<string, object> GetParameters()
        => GetPropertyValues().ToDictionary(k => k.Key, v => v.Value);

    private IEnumerable<KeyValuePair<string, object>> GetPropertyValues()
    {
        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
        {
            if (property.GetValue(this) is { } value)
            {
                var name = ToCamelCase(property.Name);
                yield return new KeyValuePair<string, object>(name, value);
            }
        }
    }

    private static string ToCamelCase(string value)
        => char.ToLowerInvariant(value[0]) + value.Substring(1);
}
