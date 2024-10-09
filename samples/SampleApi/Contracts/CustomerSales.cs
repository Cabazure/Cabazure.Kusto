namespace SampleApi.Contracts;

public record CustomerSales(
    int CustomerKey,
    string CustomerName,
    float SalesAmount,
    float TotalCost);
