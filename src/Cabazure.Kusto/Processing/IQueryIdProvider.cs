namespace Cabazure.Kusto.Processing;

public interface IQueryIdProvider
{
    string CreateQueryId(
        IKustoScript query,
        string? sessionId,
        string? nonce);

    string CreateFingerprint(
        IKustoScript query);

    string CreateFingerprint(
        Type queryType,
        string queryText,
        IEnumerable<KeyValuePair<string, string>> parameters);
}