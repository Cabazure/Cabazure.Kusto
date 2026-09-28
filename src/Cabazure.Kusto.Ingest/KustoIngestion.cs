namespace Cabazure.Kusto.Ingest;

public sealed record KustoIngestion<T>(
    string TableName,
    string MappingName,
    KustoIngestionMode Mode,
    string? ConnectionName);
