using System.Security.Cryptography;
using System.Text;

namespace Cabazure.Kusto.Processing;

public class QueryIdProvider : IQueryIdProvider
{
    private const int HashLength = 32;

    public string CreateQueryId(
        IKustoScript query,
        string? sessionId,
        string? nonce)
    {
        var queryType = query.GetType();
        var hash = Hash(queryType.FullName, sessionId, nonce);

        return string
            .Concat(queryType.Name, hash)
            .ToAlphaNumeric();
    }

    public string CreateFingerprint(
        IKustoScript query)
    {
        var values = new List<string?>
        {
            query.GetType().FullName,
            query.GetQueryText(),
        };

        foreach (var parameter in query
            .GetCslParameters()
            .OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            values.Add(parameter.Key);
            values.Add(parameter.Value);
        }

        return Hash(values.ToArray());
    }

    private static string Hash(params string?[] values)
        => Convert
            .ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(values))))
            [..HashLength];

    private static string Serialize(IEnumerable<string?> values)
    {
        var builder = new StringBuilder();
        foreach (var value in values)
        {
            if (value is null)
            {
                builder.Append("-1:");
                continue;
            }

            builder
                .Append(value.Length)
                .Append(':')
                .Append(value);
        }

        return builder.ToString();
    }
}
