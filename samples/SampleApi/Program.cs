using Azure.Identity;
using Cabazure.Kusto;
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

app.MapGet("/customers", (IKustoProcessor processor, CancellationToken cancellationToken)
        => processor.ExecuteAsync(new CustomersQuery(), cancellationToken))
    .WithName("GetCustomers")
    .WithOpenApi();

app.MapGet("/customer-sales", (IKustoProcessor processor, CancellationToken cancellationToken)
        => processor.ExecuteAsync(new CustomerSalesQuery(), cancellationToken))
    .WithName("GetCustomerSales")
    .WithOpenApi();

app.Run();
