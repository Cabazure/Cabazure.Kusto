namespace Cabazure.Kusto.Processing;

public interface IScriptHandlerFactory
{
    IScriptHandler Create(
        IKustoCommand command);

    IScriptHandler<T> Create<T>(
        IKustoQuery<T> query);

    IScriptHandler<PagedResult<T>> Create<T>(
        IKustoQuery<IReadOnlyList<T>> query,
        string? sessionId,
        int maxItemCount,
        string? continuationToken);
}
