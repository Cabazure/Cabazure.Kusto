using Azure.Identity;
using Cabazure.Kusto;
using Microsoft.AspNetCore.Mvc;
using SampleApi.Queries;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCabazureKusto(o =>
{
    o.HostAddress = "https://help.kusto.windows.net/";
    o.DatabaseName = "ContosoSales";
    o.Credential = new DefaultAzureCredential();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet(
    "/customers", 
    async static (
        [FromHeader(Name = "x-client-session-id")] string? sessionId,
        [FromHeader(Name = "x-max-item-count")] int? maxItemCount,
        [FromHeader(Name = "x-continuation-token")] string? continuationToken,
        IKustoProcessor processor, 
        CancellationToken cancellationToken)
        => await processor.ExecuteAsync(
            new CustomersQuery(),
            sessionId,
            maxItemCount ?? 100,
            continuationToken,
            cancellationToken))
    .WithName("ListCustomers")
    .WithOpenApi();

app.MapGet(
    "/customers/{customerId}",
    async static (
        int customerId,
        IKustoProcessor processor,
        CancellationToken cancellationToken)
        => await processor.ExecuteAsync(
            new CustomersQuery(
                customerId),
            cancellationToken) switch
        {
            [{ } customer] => Results.Ok(customer),
            _ => Results.NotFound(),
        })
    .WithName("GetCustomer")
    .WithOpenApi();

app.MapGet("/customer-sales", (IKustoProcessor processor, CancellationToken cancellationToken)
        => processor.ExecuteAsync(new CustomerSalesQuery(), cancellationToken))
    .WithName("GetCustomerSales")
    .WithOpenApi();

app.Run();
