using Kusto.Data.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto;

public static class KustoScriptExtensions
{
    private static readonly JsonSerializer Serializer = new JsonSerializer
    {
        Converters = { new StringEnumConverter() },
    };

    public static ClientRequestProperties GetRequestProperties(
        this IKustoScript script)
        => new(null, script.GetCslParameters())
        {
            ClientRequestId = Guid.NewGuid().ToString(),
        };

    public static IEnumerable<KeyValuePair<string, string>> GetCslParameters(
        this IKustoScript script)
        => script.GetParameters().Select(p
            => KeyValuePair.Create(
                p.Key,
                GetCslValue(p.Value)));

    public static string GetCslValue(object value)
        => value switch
        {
            bool b => CslBoolLiteral.AsCslString(b),
            int i => CslIntLiteral.AsCslString(i),
            long i => CslLongLiteral.AsCslString(i),
            decimal d => CslDecimalLiteral.AsCslString(d),
            double d => CslRealLiteral.AsCslString(d),
            TimeSpan t => CslTimeSpanLiteral.AsCslString(t),
            DateTime d => CslDateTimeLiteral.AsCslString(d),
            DateTimeOffset d => CslDateTimeLiteral.AsCslString(d.UtcDateTime),
            string s => CslStringLiteral.AsCslString(s),
            Enum e => CslStringLiteral.AsCslString(Enum.GetName(e.GetType(), e)),
            object o => CslDynamicLiteral.AsCslString(JToken.FromObject(o, Serializer)),
        };
}
