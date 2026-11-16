using Kusto.Data.Common;

namespace Cabazure.Kusto;

public sealed record KustoQueryClientContext(
    ICslQueryProvider Client,
    string DataSource,
    string? DatabaseName);