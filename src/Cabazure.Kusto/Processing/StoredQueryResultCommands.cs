using System.Globalization;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public static class StoredQueryResultCommands
{
    public const string RowNumberColumn = "row_number";
    public const string FingerprintColumn = "cabazure_fingerprint";

    public static string CreateSetOrReplaceCommand(
        IKustoScript query,
        string queryId,
        string fingerprint,
        int previewCount,
        TimeSpan expiration)
        => $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {previewCount}, expiresAfter = {(long)expiration.TotalSeconds}s) <|\n"
         + $"{query.GetQueryText().Trim(' ', '\n', '\t', ';')}\n"
         + $"| serialize {RowNumberColumn} = row_number()\n"
         + $"| extend {FingerprintColumn} = '{fingerprint}'";

    public static string CreatePageQuery(
        string queryId,
        string fingerprint,
        long itemsReturned,
        int maxItemCount)
        => $"stored_query_result('{queryId}') "
         + $"| where {FingerprintColumn} == '{fingerprint}' "
         + $"and {RowNumberColumn} between({itemsReturned + 1} .. {itemsReturned + maxItemCount})";

    public static async Task<long?> GetTotalCountAsync(
        ICslAdminProvider adminProvider,
        IKustoScript query,
        string queryId)
    {
        using var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                $".show stored_query_results ['{queryId}']",
                query.GetRequestProperties());

        if (!reader.Read())
        {
            return null;
        }

        var value = reader.GetValue(reader.GetOrdinal("RowCount"));
        return value is null or DBNull
            ? null
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }
}
