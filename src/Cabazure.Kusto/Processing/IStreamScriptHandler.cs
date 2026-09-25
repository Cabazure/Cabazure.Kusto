namespace Cabazure.Kusto.Processing;

public interface IStreamScriptHandler<T>
{
    IAsyncEnumerable<T> ExecuteAsync(
        CancellationToken cancellationToken);
}
