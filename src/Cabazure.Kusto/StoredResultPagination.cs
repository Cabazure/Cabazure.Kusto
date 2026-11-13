namespace Cabazure.Kusto;

public sealed record StoredResultPagination(
    string? SessionId,
    int MaxItemCount,
    string? ContinuationToken = null,
    bool IncludeTotalCount = false);