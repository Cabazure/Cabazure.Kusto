using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto;

public class KustoProcessor(
    IScriptHandlerFactory factory)
    : IKustoProcessor
{
    public async Task ExecuteAsync(
        IKustoCommand command,
        CancellationToken cancellationToken)
        => await factory
            .Create(command)
            .ExecuteAsync(cancellationToken);

    public async Task<T?> ExecuteAsync<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
        => await factory
            .Create(query)
            .ExecuteAsync(cancellationToken);

    public async Task<PagedResult<T>?> ExecuteAsync<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken,
        CancellationToken cancellationToken)
        => await factory
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken)
            .ExecuteAsync(cancellationToken);
}