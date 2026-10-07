using System.Runtime.CompilerServices;
using Cabazure.Kusto.Processing;
using Kusto.Data.Exceptions;
using NSubstitute.ExceptionExtensions;

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
    public async Task ExecuteAsync_Will_Create_Handler_For_PagedResult_With_TotalCount(
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
            includeTotalCount: true,
            cancellationToken);

        _ = factory
            .Received(1)
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken,
                sut.ConnectionName,
                sut.DatabaseName,
                includeTotalCount: true);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Return_TotalCount_When_Not_Paging(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<IReadOnlyList<T>> query,
        IScriptHandler<IReadOnlyList<T>> handler,
        T[] queryResult,
        CancellationToken cancellationToken)
    {
        factory
            .Create<IReadOnlyList<T>>(default!, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cancellationToken)
            .Returns(queryResult);

        var result = await sut.ExecuteAsync(
            query,
            sessionId: null,
            maxItemCount: null,
            continuationToken: null,
            includeTotalCount: true,
            cancellationToken);

        result!.Items
            .Should()
            .BeEquivalentTo(queryResult);
        result.ContinuationToken
            .Should()
            .BeNull();
        result.TotalCount
            .Should()
            .Be(queryResult.Length);
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

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Throw_OperationCanceledException_When_Command_Is_Canceled(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoCommand command,
        IScriptHandler handler)
    {
        using var cts = CreateCanceledTokenSource();
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .Create(default(IKustoCommand)!, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .ThrowsAsync(kustoException);

        var act = () => sut.ExecuteAsync(command, cts.Token);

        await ShouldBeTranslated(act, kustoException, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Throw_OperationCanceledException_When_Query_Is_Canceled(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<T> query,
        IScriptHandler<T> handler)
    {
        using var cts = CreateCanceledTokenSource();
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .Create<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .ThrowsAsync(kustoException);

        var act = () => sut.ExecuteAsync(query, cts.Token);

        await ShouldBeTranslated(act, kustoException, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Throw_OperationCanceledException_When_Paged_Query_Is_Canceled(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<IReadOnlyList<T>> query,
        string sessionId,
        int maxItemCount,
        string continuationToken,
        IScriptHandler<PagedResult<T>> handler)
    {
        using var cts = CreateCanceledTokenSource();
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .Create<T>(default!, default, default, default, default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .ThrowsAsync(kustoException);

        var act = () => sut.ExecuteAsync(
            query,
            sessionId,
            maxItemCount,
            continuationToken,
            cts.Token);

        await ShouldBeTranslated(act, kustoException, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Throw_OperationCanceledException_When_Stream_Is_Canceled(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoStreamQuery<T> query,
        IStreamScriptHandler<T> handler,
        T queryResult)
    {
        using var cts = new CancellationTokenSource();
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .CreateStream<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .Returns(_ => ReturnOneThenThrow(queryResult, cts, kustoException));

        var results = new List<T>();
        var act = async () =>
        {
            await foreach (var item in sut.ExecuteAsync(query, cts.Token))
            {
                results.Add(item);
            }
        };

        await ShouldBeTranslated(act, kustoException, cts.Token);
        results.Should().Equal(queryResult);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Throw_OperationCanceledException_When_Stream_Creation_Fails_After_Cancel(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoStreamQuery<T> query,
        IStreamScriptHandler<T> handler)
    {
        using var cts = CreateCanceledTokenSource();
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .CreateStream<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .Throws(kustoException);

        var act = () => Task.FromResult(sut.ExecuteAsync(query, cts.Token));

        await ShouldBeTranslated(act, kustoException, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Throw_OperationCanceledException_When_Stream_Enumerator_Acquisition_Fails_After_Cancel(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoStreamQuery<T> query,
        IStreamScriptHandler<T> handler,
        IAsyncEnumerable<T> source)
    {
        using var cts = CreateCanceledTokenSource();
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .CreateStream<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .Returns(source);
        source
            .GetAsyncEnumerator(cts.Token)
            .Throws(kustoException);

        var act = async () =>
        {
            await foreach (var item in sut.ExecuteAsync(query, cts.Token))
            {
                _ = item;
            }
        };

        await ShouldBeTranslated(act, kustoException, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Throw_OperationCanceledException_When_Stream_Disposal_Fails_After_Cancel(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoStreamQuery<T> query,
        IStreamScriptHandler<T> handler,
        IAsyncEnumerable<T> source,
        IAsyncEnumerator<T> enumerator,
        T queryResult)
    {
        using var cts = new CancellationTokenSource();
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .CreateStream<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .Returns(source);
        source
            .GetAsyncEnumerator(cts.Token)
            .Returns(enumerator);
        enumerator
            .MoveNextAsync()
            .Returns(new ValueTask<bool>(true));
        enumerator.Current.Returns(queryResult);
        enumerator
            .DisposeAsync()
            .Returns(_ => ValueTask.FromException(kustoException));

        var act = async () =>
        {
            await foreach (var item in sut.ExecuteAsync(query, cts.Token))
            {
                await cts.CancelAsync();
                break;
            }
        };

        await ShouldBeTranslated(act, kustoException, cts.Token);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Not_Translate_Exception_When_Not_Canceled(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<T> query,
        IScriptHandler<T> handler)
    {
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .Create<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(CancellationToken.None)
            .ThrowsAsync(kustoException);

        var act = () => sut.ExecuteAsync(query, CancellationToken.None);

        (await act.Should().ThrowAsync<KustoClientRequestCanceledByUserException>())
            .Which.Should().BeSameAs(kustoException);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Not_Translate_Stream_Exception_When_Not_Canceled(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoStreamQuery<T> query,
        IStreamScriptHandler<T> handler,
        T queryResult)
    {
        var kustoException = new KustoClientRequestCanceledByUserException();
        factory
            .CreateStream<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(CancellationToken.None)
            .Returns(_ => ReturnOneThenThrow(queryResult, cts: null, kustoException));

        var act = async () =>
        {
            await foreach (var item in sut.ExecuteAsync(query, CancellationToken.None))
            {
            }
        };

        (await act.Should().ThrowAsync<KustoClientRequestCanceledByUserException>())
            .Which.Should().BeSameAs(kustoException);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Will_Rethrow_OperationCanceledException_As_Is(
        [Frozen] IScriptHandlerFactory factory,
        [Modest] KustoProcessor sut,
        IKustoQuery<T> query,
        IScriptHandler<T> handler)
    {
        using var cts = CreateCanceledTokenSource();
        var canceledException = new OperationCanceledException(cts.Token);
        factory
            .Create<T>(default, default, default)
            .ReturnsForAnyArgs(handler);
        handler
            .ExecuteAsync(cts.Token)
            .ThrowsAsync(canceledException);

        var act = () => sut.ExecuteAsync(query, cts.Token);

        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.Should().BeSameAs(canceledException);
    }

    private static CancellationTokenSource CreateCanceledTokenSource()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts;
    }

    private static async Task ShouldBeTranslated(
        Func<Task> act,
        Exception innerException,
        CancellationToken cancellationToken)
    {
        var exception = (await act.Should().ThrowExactlyAsync<OperationCanceledException>()).Which;
        exception.InnerException.Should().BeSameAs(innerException);
        exception.CancellationToken.Should().Be(cancellationToken);
    }

    private static async IAsyncEnumerable<T> ReturnOneThenThrow(
        T item,
        CancellationTokenSource? cts,
        Exception exception)
    {
        yield return item;
        await Task.Yield();
        cts?.Cancel();
        throw exception;
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
