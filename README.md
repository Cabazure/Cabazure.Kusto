[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Cabazure/Cabazure.Kusto/.github%2Fworkflows%2Fci.yml)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)
[![GitHub Release Date](https://img.shields.io/github/release-date/Cabazure/Cabazure.Kusto)](https://github.com/Cabazure/Cabazure.Kusto/releases)
[![NuGet Version](https://img.shields.io/nuget/v/Cabazure.Kusto?color=blue)](https://www.nuget.org/packages/Cabazure.Kusto)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cabazure.Kusto?color=blue)](https://www.nuget.org/stats/packages/Cabazure.Kusto?groupby=Version)

[![Branch Coverage](https://raw.githubusercontent.com/Cabazure/Cabazure.Kusto/main/.github/coveragereport/badge_branchcoverage.svg?raw=true)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)
[![Line Coverage](https://raw.githubusercontent.com/Cabazure/Cabazure.Kusto/main/.github/coveragereport/badge_linecoverage.svg?raw=true)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)
[![Method Coverage](https://raw.githubusercontent.com/Cabazure/Cabazure.Kusto/main/.github/coveragereport/badge_methodcoverage.svg?raw=true)](https://github.com/Cabazure/Cabazure.Kusto/actions/workflows/ci.yml)

# Cabazure.Kusto

Cabazure.Kusto is a library for handling and executing Kusto scripts against an Azure Data Explorer cluster.

The library extents the official .NET SDK, and adds functionality for:
 * Handling embedded .kusto scripts in your .NET projects
 * Passing parameters to your .kusto scripts
 * Deserialization of query results
 * Pagination using stored query results

## Getting started

### 1. Configuring the Cabazure.Kusto library

The Cabazure.Kusto is initialized by calling the `AddCabazureKusto()` on the `IServiceCollection` during startup of your application, like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCabazureKusto(o =>
{
    o.HostAddress = "https://help.kusto.windows.net/";
    o.DatabaseName = "ContosoSales";
    o.Credential = new DefaultAzureCredential();
});
```

_Note: The `CabazureKustoOptions` can also be configured using the `Microsoft.Extensions.Options` framework, by registering an implementation of `IConfigureOptions<CabazureKustoOptions>`. In this case, it can be omitted on the `AddCabazureKusto()` call._

### 2. Adding a Kusto query

A Kusto query is added by creating two files to your project:

  * A `.kusto` script file containing the Kusto query itself, added with "Build Action" set to "Embedded resource"
  * A .NET record with the same name (and namespace) as the embedded `.kusto` script.

The .NET record should to derive from one of the following base types:

| Base type       | Description                                            |
| --------------- | ------------------------------------------------------ |
| `KustoCommand`  | Used for Kusto commands that do not produce an output. |
| `KustoQuery<T>` | Used for Kusto queries that returns a result.          |

_Note: The base types handles the loading of the embedded `.kusto` script file, passing of parameters and deserialization of the output._

Parameters are specified by adding them to record, and declare them at the top of the `.kusto` script, like this:

```csharp
// file: CustomerQuery.cs
public record CustomerQuery(
  string CustomerType)
  : KustoQuery<Customer>;
```

```kusto
// file: CustomerQuery.kusto
declare query_parameters (
    customerType:string)
;
Customers
| where type == customerType
| project
    customerId,
    name,
    type,
    lastUpdated = timestamp,
```

The query result is mapped to the specified output contract, my matching parameter names like this:

```csharp
// file: Customer.cs
public record Customer(
  string CustomerId,
  string Name,
  CustomerType Type,
  DateTimeOffset LastUpdated);
```

### 3. Execute a Kusto query

Kusto scripts can be executed using the `IKustoProcessor` registered in the Dependency Injection container, like this:

```csharp
app.MapGet(
  "/customers",
  (IKustoProcessor processor, CancellationToken cancellationToken)
    => processor
      .ExecuteAsync(
        new CustomerQuery("type"),
        cancellationToken));
```

The processor can also perform pagination by using the `ExecuteAsync` overload, taking in a session id, a continuation token and a max item count, like this:

```csharp
app.MapGet(
  "/customers",
  ([FromHeader(Name = "x-max-item-count")] int? maxItemCount,
  [FromHeader(Name = "x-continuation")] string? continuation,
  [FromHeader(Name = "x-session-id")] string? sessionId,
  IKustoProcessor processor,
  CancellationToken cancellationToken)
    => processor
      .ExecuteAsync(
        new CustomerQuery("type"),
        sessionId,
        maxItemCount,
        continuationToken,
        cancellationToken));
```

The `maxItemCount` specifies how many items to return for each page. Each page is returned with a `continuationToken` that can be specified to fetch the next page.

The optional `sessionId` can be provided to optimize the use of storage on the ADX. If the same `sessionId` is specified for two calls they will share the underlying storage for pagination results.

## Sample

Please see the [SampleApi project](https://github.com/Cabazure/Cabazure.Kusto/tree/main/samples/SampleApi), for an example of how Cabazure.Kusto can be setup to query the "ContosoSales" database of the ADX sample cluster.
