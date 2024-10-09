using Cabazure.Kusto;
using SampleApi.Contracts;

namespace SampleApi.Queries;

public record CustomerSalesQuery
    : KustoQuery<CustomerSales>
{
}
