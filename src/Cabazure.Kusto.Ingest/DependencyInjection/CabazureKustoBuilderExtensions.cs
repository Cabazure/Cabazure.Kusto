using Cabazure.Kusto.Ingest;
using Microsoft.Extensions.DependencyInjection;

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

        builder.Services.AddOptions<CabazureKustoIngestOptions>();
        builder.Services.AddSingleton(
            new KustoIngestion<T>(
                tableName,
                mappingName,
                mode,
                builder.ConnectionName));

        return builder;
    }
}
