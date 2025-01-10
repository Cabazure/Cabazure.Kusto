using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto;

public class KustoProcessor(
    IScriptHandlerFactory factory,
    string? connectionName,
    string? databaseName)
    : IKustoProcessor
{
    public string? ConnectionName { get; }
        = connectionName;

    public string? DatabaseName { get; }
        = databaseName;

    public async Task ExecuteAsync(
        IKustoCommand command,
        CancellationToken cancellationToken)
        => await factory
            .Create(
                command,
                ConnectionName,
                DatabaseName)
            .ExecuteAsync(cancellationToken);

    public async Task<T?> ExecuteAsync<T>(
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
        => await factory
            .Create(
                query,
                ConnectionName,
                DatabaseName)
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
                continuationToken,
                ConnectionName,
                DatabaseName)
            .ExecuteAsync(cancellationToken);
}
