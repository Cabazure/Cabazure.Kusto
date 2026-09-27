using System.Globalization;

namespace Cabazure.Kusto.Processing;

public record StoredQueryContinuationToken(
    string? Nonce,
    long ItemsReturned,
    long? TotalCount)
{
    private const string Version = "v2";
    private const int MaxNonceLength = 64;

    public static StoredQueryContinuationToken? Parse(string? token)
    {
        var split = token?.Split(';');
        if (split is not { Length: 3 or 4 }
            || split[0] != Version
            || split[1].Length > MaxNonceLength
            || split[1].ToAlphaNumeric() != split[1]
            || !TryParseCount(split[2], out var itemsReturned))
        {
            return null;
        }

        long? totalCount = null;
        if (split.Length == 4)
        {
            if (!TryParseCount(split[3], out var total))
            {
                return null;
            }

            totalCount = total;
        }

        return new(
            split[1] is { Length: > 0 } nonce ? nonce : null,
            itemsReturned,
            totalCount);
    }

    public override string ToString()
        => TotalCount is { } total
         ? $"{Version};{Nonce};{ItemsReturned};{total}"
         : $"{Version};{Nonce};{ItemsReturned}";

    public static StoredQueryContinuationToken? CreateNext(
        string? nonce,
        long itemsReturned,
        int pageCount,
        int maxItemCount,
        long? totalCount)
        => pageCount < maxItemCount || itemsReturned >= totalCount
         ? null
         : new(nonce, itemsReturned, totalCount);

    private static bool TryParseCount(string value, out long count)
        => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count);
}
