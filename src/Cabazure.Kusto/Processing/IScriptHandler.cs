namespace Cabazure.Kusto.Processing;

public interface IScriptHandler<T>
{
    Task<T?> ExecuteAsync(
        CancellationToken cancellationToken);
}

public interface IScriptHandler
{
    Task ExecuteAsync(
        CancellationToken cancellationToken);
}
