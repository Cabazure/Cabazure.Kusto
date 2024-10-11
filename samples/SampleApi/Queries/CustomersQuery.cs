using Cabazure.Kusto;
using SampleApi.Contracts;

namespace SampleApi.Queries;

public record CustomersQuery(
    int? CustomerId = null)
    : KustoQuery<Customer>;
