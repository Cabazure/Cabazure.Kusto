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

    // Note: `ClientRequestProperties.OptionResultsProgressiveEnabled` is a
    // v2-only ("v2/rest/query") option; ICslQueryProvider.ExecuteQueryAsync
    // always issues requests against the v1 endpoint ("v1/rest/query"),
    // which rejects requests with that option set ("Progressive query
    // results are not supported for api_version=v1"). Row-by-row streaming
    // of v1 results is instead controlled by KustoConnectionStringBuilder's
    // `Streaming` setting (true by default), which StreamQueryHandler relies
    // on implicitly - no request-level option is needed or supported here.
    public static ClientRequestProperties GetRequestProperties(
        this IKustoScript script)
        => new(
            null,
            script.GetCslParameters())
        {
            ClientRequestId = Guid.NewGuid().ToString(),
        };

    public static IEnumerable<KeyValuePair<string, string>> GetCslParameters(
        this IKustoScript script)
        => script.GetParameters().Select(p
            => new KeyValuePair<string, string>(
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
