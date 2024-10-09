using Cabazure.Kusto;
using SampleApi.Contracts;

namespace SampleApi.Queries;

public record CustomersQuery : KustoQuery<Customer>;
