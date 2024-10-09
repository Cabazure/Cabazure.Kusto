namespace Cabazure.Kusto;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    string? ContinuationToken);
