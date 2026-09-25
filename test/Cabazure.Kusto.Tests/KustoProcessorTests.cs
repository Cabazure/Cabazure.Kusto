using System.Runtime.CompilerServices;
using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests;

public class KustoProcessorTests
{
    public record T();

    [Theory, AutoNSubstituteData]
    public void Can_Specify_Connection_And_Database(
        IScriptHandlerFactory factory,
        string connectionName,
        string databaseName)
    {
        var sut = new KustoProcessor(
            factory,
            connectionName,
            databaseName);

        sut.ConnectionName.Should().Be(connectionName);
        sut.DatabaseName.Should().Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Create_Handler(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
    {
        await sut.ExecuteAsync(query, cancellationToken);

        _ = factory
            .Received(1)
            .Create(
                query,
                sut.ConnectionName,
                sut.DatabaseName);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Create_Handler_With_Connection_And_Database(
        [Frozen] IScriptHandlerFactory factory,
        [Greedy] KustoProcessor sut,
        IKustoQuery<T> query,
        CancellationToken cancellationToken)
    {
        await sut.ExecuteAsync(query, cancellationToken);

        _ = factory
            .Received(1)
            .Create(
                query,
                sut.ConnectionName,
                sut.DatabaseName);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Return_Result_From_Handler(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<T> query,
        IScriptHandler<T> handler,
        T queryResult,
        CancellationToken cancellationToken)
    {
        factory
            .Create<T>(default, default, default)
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
        [Modest] KustoProcessor sut,
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
                continuationToken,
                sut.ConnectionName,
                sut.DatabaseName);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Return_PagedResult_From_Handler(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<IReadOnlyList<T>> query,
        string sessionId,
        int maxItemCount,
        string continuationToken,
        IScriptHandler<PagedResult<T>> handler,
        PagedResult<T> queryResult,
        CancellationToken cancellationToken)
    {
        factory
            .Create<T>(default, default, default, default, default, default)
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

    [Theory, AutoNSubstituteData]
    public void ExecuteAsync_Will_Create_Stream_Handler(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoStreamQuery<T> query,
        CancellationToken cancellationToken)
    {
        _ = sut.ExecuteAsync(query, cancellationToken);

        _ = factory
            .Received(1)
            .CreateStream(
                query,
                sut.ConnectionName,
                sut.DatabaseName);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Return_Stream_From_Handler(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoStreamQuery<T> query,
        IStreamScriptHandler<T> handler,
        T queryResult,
        CancellationToken cancellationToken)
    {
        factory
            .CreateStream<T>(default, default, default)
            .ReturnsForAnyArgs(handler);

        handler
            .ExecuteAsync(cancellationToken)
            .Returns(_ => ReturnOne(queryResult, cancellationToken));

        var results = new List<T>();
        await foreach (var item in sut.ExecuteAsync(query, cancellationToken))
        {
            results.Add(item);
        }

        results.Should().Equal(queryResult);
    }

    private static async IAsyncEnumerable<T> ReturnOne(
        T item,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return item;
        await Task.CompletedTask;
    }
}
