namespace Cabazure.Kusto.Ingest;

public sealed record KustoIngestionResult(
    Guid SourceId,
    KustoIngestionMethod Method,
    KustoIngestionStatus Status);
