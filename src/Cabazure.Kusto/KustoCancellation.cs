using System.Runtime.CompilerServices;

namespace Cabazure.Kusto;

/// <summary>
/// The Kusto SDK reports a canceled request with its own exception types (e.g.
/// <c>KustoClientRequestCanceledByUserException</c>) or transport exceptions, rather than
/// an <see cref="OperationCanceledException"/>. This translates them, so callers and hosts
/// (like ASP.NET Core) can recognize caller cancellation and treat it as such.
/// </summary>
internal static class KustoCancellation
{
    private const string CanceledMessage = "The Kusto request was canceled.";

    public static bool IsCanceledBy(
        this Exception exception,
        CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested
        && exception is not OperationCanceledException;

    public static OperationCanceledException ToOperationCanceled(
        this Exception exception,
        CancellationToken cancellationToken)
        => new(CanceledMessage, exception, cancellationToken);

    public static async IAsyncEnumerable<T> WithCancellationTranslation<T>(
        this IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Exception ex) when (ex.IsCanceledBy(cancellationToken))
                {
                    throw ex.ToOperationCanceled(cancellationToken);
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}
