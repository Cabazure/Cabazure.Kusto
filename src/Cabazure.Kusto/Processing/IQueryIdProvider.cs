namespace Cabazure.Kusto.Processing;

public interface IQueryIdProvider
{
    string CreateQueryId(
        IKustoScript query,
        string? sessionId,
        string? nonce);

    string CreateFingerprint(
        IKustoScript query);
}
