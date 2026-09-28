using System.Text.Json;
using System.Runtime.CompilerServices;
using Cabazure.Kusto.Ingest.Internal;
using Kusto.Data.Common;
using Kusto.Data.Ingestion;
using Kusto.Ingest;

namespace Cabazure.Kusto.Ingest.Tests.Internal;

public class KustoIngesterTests
{
    public record Item(string Value);

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Serializes_Items_As_Json_Lines(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        string? payload = null;
        StreamSourceOptions? sourceOptions = null;
        KustoIngestionProperties? properties = null;
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(call => ConsumeAsync(
                call,
                sdkResult,
                value => payload = value,
                value => properties = value,
                value => sourceOptions = value));
        sdkResult
            .GetIngestionStatusBySourceId(Arg.Any<Guid>())
            .Returns(call => new IngestionStatus(call.Arg<Guid>())
            {
                Status = Status.Succeeded,
            });
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);

        KustoIngestionResult result = await sut.IngestAsync(
            [
                new Item("one"),
                new Item("two"),
            ]);

        payload.Should().Be(
            "{\"value\":\"one\"}\n{\"value\":\"two\"}\n");
        properties.Should().NotBeNull();
        properties!.DatabaseName.Should().Be(databaseName);
        properties.TableName.Should().Be(tableName);
        properties.Format.Should().Be(DataSourceFormat.json);
        properties.IngestionMapping.IngestionMappingKind
            .Should()
            .Be(IngestionMappingKind.Json);
        properties.IngestionMapping.IngestionMappingReference
            .Should()
            .Be(mappingName);
        result.SourceId.Should().Be(sourceOptions!.SourceId);
        result.Method.Should().Be(KustoIngestionMethod.Streaming);
        result.Status.Should().Be(KustoIngestionStatus.Succeeded);
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Maps_Queued_Result(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.Queued)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(call => ConsumeAsync(
                call,
                sdkResult));
        sdkResult
            .GetIngestionStatusBySourceId(Arg.Any<Guid>())
            .Returns(call => new IngestionStatus(call.Arg<Guid>())
            {
                Status = Status.Queued,
            });
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName,
            KustoIngestionMode.Queued);

        KustoIngestionResult result = await sut.IngestAsync(
            [new Item("one")]);

        result.Method.Should().Be(KustoIngestionMethod.Queued);
        result.Status.Should().Be(KustoIngestionStatus.Queued);
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Uses_Custom_Serializer_Options(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        string? payload = null;
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(call => ConsumeAsync(
                call,
                sdkResult,
                value => payload = value));
        sdkResult
            .GetIngestionStatusBySourceId(Arg.Any<Guid>())
            .Returns(call => new IngestionStatus(call.Arg<Guid>())
            {
                Status = Status.Succeeded,
            });
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
        };
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName,
            serializerOptions: serializerOptions);

        await sut.IngestAsync([new Item("one")]);

        payload.Should().Be("{\"Value\":\"one\"}\n");
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Propagates_Enumeration_Failure(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName,
        InvalidOperationException exception)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(call => ConsumeAsync(
                call,
                sdkResult));
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);

        Func<Task> act = () => sut.IngestAsync(
            ThrowingItems(exception));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .Where(e => e == exception);
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Streams_Before_Source_Completes(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        var firstRecordRead = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        string? firstLine = null;
        string? secondLine = null;
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(async call =>
            {
                using var reader = new StreamReader(
                    call.ArgAt<Stream>(0));
                firstLine = await reader.ReadLineAsync();
                firstRecordRead.SetResult();
                secondLine = await reader.ReadLineAsync();
                return sdkResult;
            });
        sdkResult
            .GetIngestionStatusBySourceId(Arg.Any<Guid>())
            .Returns(call => new IngestionStatus(call.Arg<Guid>())
            {
                Status = Status.Succeeded,
            });
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(5));

        await sut.IngestAsync(
            StreamingItems(
                firstRecordRead.Task,
                timeout.Token),
            timeout.Token);

        firstLine.Should().Be("{\"value\":\"one\"}");
        secondLine.Should().Be("{\"value\":\"two\"}");
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Maps_Empty_Input_As_Skipped(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(call => ConsumeAsync(
                call,
                sdkResult));
        sdkResult
            .GetIngestionStatusBySourceId(Arg.Any<Guid>())
            .Returns(call => new IngestionStatus(call.Arg<Guid>())
            {
                Status = Status.Skipped,
            });
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);

        KustoIngestionResult result = await sut.IngestAsync([]);

        result.Status.Should().Be(KustoIngestionStatus.Skipped);
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Propagates_Sdk_Failure(
        IKustoIngestClient client,
        string tableName,
        string mappingName,
        string databaseName,
        InvalidOperationException exception)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(Task.FromException<IKustoIngestionResult>(
                exception));
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(5));
        Func<Task> act = () => sut.IngestAsync(
            EndlessItems(timeout.Token),
            timeout.Token);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .Where(e => e == exception);
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Cancels_Producer_When_Client_Resolution_Fails(
        string tableName,
        string mappingName,
        string databaseName,
        InvalidOperationException exception)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(_ => throw exception);
        var enumerationCompleted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);

        Func<Task> act = () => sut.IngestAsync(
            TrackedEndlessItems(enumerationCompleted));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .Where(e => e == exception);
        enumerationCompleted.Task.IsCompleted.Should().BeTrue();
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Cancels_Producer_When_Sdk_Setup_Fails(
        IKustoIngestClient client,
        string tableName,
        string mappingName,
        string databaseName,
        InvalidOperationException exception)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .When(x => x.IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>()))
            .Do(_ => throw exception);
        var enumerationCompleted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);

        Func<Task> act = () => sut.IngestAsync(
            TrackedEndlessItems(enumerationCompleted));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .Where(e => e == exception);
        enumerationCompleted.Task.IsCompleted.Should().BeTrue();
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Throws_For_Failed_Status(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName,
        string details)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(call => ConsumeAsync(
                call,
                sdkResult));
        sdkResult
            .GetIngestionStatusBySourceId(Arg.Any<Guid>())
            .Returns(call => new IngestionStatus(call.Arg<Guid>())
            {
                Status = Status.Failed,
                Details = details,
            });
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);

        Func<Task> act = () => sut.IngestAsync(
            [new Item("one")]);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{details}*");
    }

    [Theory, AutoNSubstituteData]
    public async Task IngestAsync_Observes_Cancellation(
        IKustoIngestClient client,
        IKustoIngestionResult sdkResult,
        string tableName,
        string mappingName,
        string databaseName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        clientProvider
            .GetClient(null, KustoIngestionMode.ManagedStreaming)
            .Returns(client);
        client
            .IngestFromStreamAsync(
                Arg.Any<Stream>(),
                Arg.Any<KustoIngestionProperties>(),
                Arg.Any<StreamSourceOptions>())
            .Returns(call => ConsumeAsync(
                call,
                sdkResult));
        var sut = CreateSut(
            clientProvider,
            tableName,
            mappingName,
            databaseName);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        Func<Task> act = () => sut.IngestAsync(
            [new Item("one")],
            cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static KustoIngester<Item> CreateSut(
        IKustoIngestClientProvider clientProvider,
        string tableName,
        string mappingName,
        string databaseName,
        KustoIngestionMode mode = KustoIngestionMode.ManagedStreaming,
        JsonSerializerOptions? serializerOptions = null)
        => new(
            clientProvider,
            new KustoIngestion<Item>(
                tableName,
                mappingName,
                mode),
            serializerOptions ?? new(JsonSerializerDefaults.Web),
            connectionName: null,
            databaseName,
            mode);

    private static async Task<IKustoIngestionResult> ConsumeAsync(
        NSubstitute.Core.CallInfo call,
        IKustoIngestionResult result,
        Action<string>? setPayload = null,
        Action<KustoIngestionProperties>? setProperties = null,
        Action<StreamSourceOptions>? setSourceOptions = null)
    {
        Stream stream = call.ArgAt<Stream>(0);
        setProperties?.Invoke(
            call.ArgAt<KustoIngestionProperties>(1));
        setSourceOptions?.Invoke(
            call.ArgAt<StreamSourceOptions>(2));
        using var reader = new StreamReader(stream);
        string payload = await reader.ReadToEndAsync();
        setPayload?.Invoke(payload);
        return result;
    }

    private static async IAsyncEnumerable<Item> ThrowingItems(
        Exception exception)
    {
        yield return new Item("one");
        await Task.Yield();
        throw exception;
    }

    private static async IAsyncEnumerable<Item> StreamingItems(
        Task firstRecordRead,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new Item("one");
        await firstRecordRead.WaitAsync(cancellationToken);
        yield return new Item("two");
    }

    private static async IAsyncEnumerable<Item> EndlessItems(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new Item("value");
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Item> TrackedEndlessItems(
        TaskCompletionSource enumerationCompleted,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new Item("value");
                await Task.Yield();
            }
        }
        finally
        {
            enumerationCompleted.SetResult();
        }
    }
}
