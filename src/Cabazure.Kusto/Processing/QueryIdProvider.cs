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
        var hash = Hash($"{queryType.FullName}\n{sessionId}\n{nonce}");

        return string
            .Concat(queryType.Name, hash)
            .ToAlphaNumeric();
    }

    public string CreateFingerprint(
        IKustoScript query)
    {
        var builder = new StringBuilder()
            .Append(query.GetType().FullName)
            .Append('\n')
            .Append(query.GetQueryText());

        foreach (var parameter in query
            .GetCslParameters()
            .OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            builder
                .Append('\n')
                .Append(parameter.Key)
                .Append('=')
                .Append(parameter.Value);
        }

        return Hash(builder.ToString());
    }

    private static string Hash(string value)
        => Convert
            .ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            [..HashLength];
}
