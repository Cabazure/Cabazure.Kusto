namespace SampleApi.Contracts;

public record CustomerSales(
    int CustomerKey,
    string CustomerName,
    decimal SalesAmount,
    decimal TotalCost);
