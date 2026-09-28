using Cabazure.Kusto.Ingest;
using Cabazure.Kusto.Ingest.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cabazure.Kusto.DependencyInjection;

public static class CabazureKustoBuilderExtensions
{
    public static CabazureKustoBuilder AddIngestion<T>(
        this CabazureKustoBuilder builder,
        string tableName,
        string mappingName,
        KustoIngestionMode mode = KustoIngestionMode.ManagedStreaming)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mappingName);

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unsupported Kusto ingestion mode.");
        }

        if (builder.Services.Any(d
            => d.ServiceType == typeof(KustoIngestion<T>)))
        {
            throw new InvalidOperationException(
                $"An ingestion is already registered for `{typeof(T).FullName}`.");
        }

        builder.Services
            .TryAddSingleton<IKustoIngestClientFactory, KustoIngestClientFactory>();
        builder.Services
            .TryAddSingleton<IKustoIngestClientProvider, KustoIngestClientProvider>();
        builder.Services
            .TryAddSingleton<IKustoIngesterFactory, KustoIngesterFactory>();
        builder.Services.AddSingleton(
            new KustoIngestion<T>(
                tableName,
                mappingName,
                mode,
                builder.ConnectionName));
        builder.Services.AddSingleton(s => s
            .GetRequiredService<IKustoIngesterFactory>()
            .Create<T>());

        return builder;
    }
}
