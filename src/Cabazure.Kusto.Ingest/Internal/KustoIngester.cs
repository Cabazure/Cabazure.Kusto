using System.IO.Pipelines;
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

    public Task<KustoIngestionResult> IngestAsync(
        IEnumerable<T> items,
        CancellationToken cancellationToken = default)
        => IngestAsync(
            ToAsyncEnumerable(items, cancellationToken),
            cancellationToken);

    public async Task<KustoIngestionResult> IngestAsync(
        IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var sourceId = Guid.NewGuid();
        var pipe = new Pipe();
        Task producer = SerializeAsync(
            items,
            pipe.Writer,
            cancellationToken);
        using Stream sourceStream = pipe.Reader.AsStream();
        Task<IKustoIngestionResult> consumer = clientProvider
            .GetClient(ConnectionName, Mode)
            .IngestFromStreamAsync(
                sourceStream,
                CreateIngestionProperties(),
                new StreamSourceOptions
                {
                    SourceId = sourceId,
                });

        try
        {
            await Task.WhenAll(producer, consumer);
        }
        catch
        {
            await pipe.Reader.CompleteAsync();
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }

        IKustoIngestionResult sdkResult = await consumer;
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

    private async Task SerializeAsync(
        IAsyncEnumerable<T> items,
        PipeWriter writer,
        CancellationToken cancellationToken)
    {
        Exception? exception = null;
        try
        {
            await using Stream stream = writer.AsStream(leaveOpen: true);
            await foreach (T item in items
                .WithCancellation(cancellationToken))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    item,
                    serializerOptions,
                    cancellationToken);
                await stream.WriteAsync(
                    "\n"u8.ToArray(),
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            await writer.CompleteAsync(exception);
        }
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable(
        IEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (T item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}
