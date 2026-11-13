using System.Buffers.Text;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cabazure.Kusto.Processing;

internal sealed record KeysetContinuationToken(
    string Fingerprint,
    ImmutableArray<KeysetCursorValue> Values)
{
    private const string Prefix = "ks1.";
    private const int MaxTokenLength = 65536;

    private static readonly JsonSerializerOptions TokenOptions = new()
    {
        MaxDepth = 8,
    };

    public override string ToString()
    {
        var token = Prefix + Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(this, TokenOptions));
        return token.Length <= MaxTokenLength
            ? token
            : throw new InvalidOperationException("The keyset continuation token exceeds 64 KiB.");
    }

    public static KeysetContinuationToken? Parse(
        string? token,
        string expectedFingerprint,
        int keyCount)
    {
        if (token is null
            || token.Length > MaxTokenLength
            || !token.StartsWith(Prefix, StringComparison.Ordinal)
            || keyCount <= 0)
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<KeysetContinuationToken>(
                Base64Url.DecodeFromChars(token.AsSpan(Prefix.Length)),
                TokenOptions);

            if (parsed is null
                || !string.Equals(parsed.Fingerprint, expectedFingerprint, StringComparison.Ordinal)
                || parsed.Values.IsDefault
                || parsed.Values.Length != keyCount)
            {
                return null;
            }

            foreach (var value in parsed.Values)
            {
                if (value is null)
                {
                    return null;
                }

                _ = value.GetCslValue();
            }

            return parsed;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or OverflowException or ArgumentException or SqlTypeException)
        {
            return null;
        }
    }

    public static string CreateFingerprint(
        string queryFingerprint,
        IReadOnlyList<KustoOrder.Column> columns,
        string? connectionScope,
        string? databaseName)
        => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Query = queryFingerprint,
            Columns = columns,
            Connection = connectionScope,
            Database = databaseName,
        })));
}