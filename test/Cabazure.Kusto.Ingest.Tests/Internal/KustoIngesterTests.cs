using System.Text.Json;
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
                mode,
                ConnectionName: null),
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
}
