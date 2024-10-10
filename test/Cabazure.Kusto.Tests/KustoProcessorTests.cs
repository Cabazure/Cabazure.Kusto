using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests;

public class KustoProcessorTests
{
    public record T();

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Create_Handler(
        [Frozen] IScriptHandlerFactory factory,
        KustoProcessor sut,
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
    {
        await sut.ExecuteAsync(query, cancellationToken);

        _ = factory
            .Received(1)
            .Create(query);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Return_Result_From_Handler(
        [Frozen] IScriptHandlerFactory factory,
        KustoProcessor sut,
        IKustoQuery<T> query,
        IScriptHandler<T> handler,
        T queryResult,
        CancellationToken cancellationToken)
    {
        factory
            .Create<T>(default)
            .ReturnsForAnyArgs(handler);

        handler
            .ExecuteAsync(cancellationToken)
            .Returns(queryResult);

        var result = await sut.ExecuteAsync(query, cancellationToken);
        result
            .Should()
            .Be(queryResult);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Create_Handler_For_PagedResult(
        [Frozen] IScriptHandlerFactory factory,
        KustoProcessor sut,
        IKustoQuery<IReadOnlyList<T>> query,
        string sessionId,
        int maxItemCount,
        string continuationToken,
        CancellationToken cancellationToken)
    {
        await sut.ExecuteAsync(
            query,
            sessionId,
            maxItemCount,
            continuationToken,
            cancellationToken);

        _ = factory
            .Received(1)
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Return_PagedResult_From_Handler(
        [Frozen] IScriptHandlerFactory factory,
        KustoProcessor sut,
        IKustoQuery<IReadOnlyList<T>> query,
        string sessionId,
        int maxItemCount,
        string continuationToken,
        IScriptHandler<PagedResult<T>> handler,
        PagedResult<T> queryResult,
        CancellationToken cancellationToken)
    {
        factory
            .Create<T>(default, default, default, default)
            .ReturnsForAnyArgs(handler);

        handler
            .ExecuteAsync(cancellationToken)
            .Returns(queryResult);

        var result = await sut.ExecuteAsync(
            query,
            sessionId,
            maxItemCount,
            continuationToken,
            cancellationToken);

        result
            .Should()
            .Be(queryResult);
    }
}
