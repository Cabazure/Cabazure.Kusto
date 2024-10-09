namespace Cabazure.Kusto;

public interface IKustoProcessor
{
    Task ExecuteAsync(
        IKustoCommand command,
        CancellationToken cancellationToken);

    Task<T?> ExecuteAsync<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken);

    Task<PagedResult<T>?> ExecuteAsync<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken,
        CancellationToken cancellationToken);
}