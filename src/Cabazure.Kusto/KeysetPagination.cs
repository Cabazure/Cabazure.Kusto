namespace Cabazure.Kusto;

public sealed record KeysetPagination(
    KustoOrder Order,
    int MaxItemCount,
    string? ContinuationToken = null);