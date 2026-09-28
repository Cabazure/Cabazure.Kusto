using System.Runtime.CompilerServices;
using System.Text.Json;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Ingestion;
using Kusto.Ingest;

namespace Cabazure.Kusto.Ingest.Internal;

internal class KustoIngester<T>(
    IKustoIngestClientProvider clientProvider,
    KustoIngestion<T> ingestion,
    JsonSerializerOptions serializerOptions,
    string? connectionName,
    string databaseName,
    KustoIngestionMode mode)
    : IKustoIngester<T>
{
    internal string? ConnectionName { get; } = connectionName;

    internal string DatabaseName { get; } = databaseName;

    internal KustoIngestionMode Mode { get; } = mode;

    internal JsonSerializerOptions SerializerOptions { get; }
        = serializerOptions;

    public Task<KustoIngestionResult> IngestAsync(
        IEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        return IngestAsync(
            ToAsyncEnumerable(items, cancellationToken),
            cancellationToken);
    }

    public async Task<KustoIngestionResult> IngestAsync(
        IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var sourceId = Guid.NewGuid();
        await using FileStream sourceStream = CreateTemporaryStream();
        await SerializeAsync(
            items,
            sourceStream,
            cancellationToken);
        sourceStream.Position = 0;

        IKustoIngestionResult sdkResult = await clientProvider
            .GetClient(ConnectionName, Mode)
            .IngestFromStreamAsync(
                sourceStream,
                CreateIngestionProperties(),
                new StreamSourceOptions
                {
                    SourceId = sourceId,
                },
                cancellationToken);
        IngestionStatus status = sdkResult
            .GetIngestionStatusBySourceId(sourceId);

        return MapResult(sourceId, status);
    }

    private KustoIngestionProperties CreateIngestionProperties()
        => new(DatabaseName, ingestion.TableName)
        {
            Format = DataSourceFormat.json,
            IngestionMapping = new IngestionMapping
            {
                IngestionMappingKind = IngestionMappingKind.Json,
                IngestionMappingReference = ingestion.MappingName,
            },
        };

    private KustoIngestionResult MapResult(
        Guid sourceId,
        IngestionStatus status)
        => status.Status switch
        {
            Status.Succeeded => new(
                sourceId,
                Mode == KustoIngestionMode.Queued
                    ? KustoIngestionMethod.Queued
                    : KustoIngestionMethod.Streaming,
                KustoIngestionStatus.Succeeded),
            Status.Pending or Status.Queued => new(
                sourceId,
                KustoIngestionMethod.Queued,
                KustoIngestionStatus.Queued),
            Status.Skipped => new(
                sourceId,
                Mode == KustoIngestionMode.Queued
                    ? KustoIngestionMethod.Queued
                    : KustoIngestionMethod.Streaming,
                KustoIngestionStatus.Skipped),
            _ => throw new InvalidOperationException(
                $"Kusto ingestion failed with status `{status.Status}`: {status.Details}"),
        };

    private static FileStream CreateTemporaryStream()
        => new(
            Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous
                | FileOptions.DeleteOnClose
                | FileOptions.SequentialScan);

    private async Task SerializeAsync(
        IAsyncEnumerable<T> items,
        Stream stream,
        CancellationToken cancellationToken)
    {
        await foreach (T item in items
            .WithCancellation(cancellationToken))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                item,
                SerializerOptions,
                cancellationToken);
            await stream.WriteAsync(
                "\n"u8.ToArray(),
                cancellationToken);
        }

        await stream.FlushAsync(cancellationToken);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable(
        IEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (T item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}
