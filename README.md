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
 * Typed streaming and queued ingestion through the optional `Cabazure.Kusto.Ingest` package

## Getting started

### 1. Configuring the Cabazure.Kusto library

Cabazure.Kusto is initialized by calling `AddCabazureKusto()` on the `IServiceCollection` during application startup:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCabazureKusto(kusto => kusto
  .Configure(o => o
    .WithHostAddress("https://help.kusto.windows.net/")
    .WithDatabaseName("ContosoSales")
    .WithCredential(new DefaultAzureCredential())));
```

The builder can also register an `IConfigureOptions<CabazureKustoOptions>` or `IConfigureNamedOptions<CabazureKustoOptions>` implementation:

```csharp
builder.Services.AddCabazureKusto(kusto => kusto
  .Configure<ConfigureKustoOptions>());
```

When options are registered separately, the builder callback can be omitted:

```csharp
builder.Services.ConfigureOptions<ConfigureKustoOptions>();
builder.Services.AddCabazureKusto();
```

Named connections use the overload taking a connection name:

```csharp
builder.Services.AddCabazureKusto(
  "analytics",
  kusto => kusto.Configure<ConfigureKustoOptions>());
```

The named overload also supports external `IConfigureNamedOptions<CabazureKustoOptions>` configuration:

```csharp
builder.Services.ConfigureOptions<ConfigureKustoOptions>();
builder.Services.AddCabazureKusto("analytics");
```

The connection can use `WithConnectionString()` instead of `WithHostAddress()`. In both cases, `WithDatabaseName()` sets the default database. A query can override the connection or database through `IKustoProcessorFactory.Create()`.

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

The `maxItemCount` specifies how many items to return for each page. Each page is returned with a `continuationToken` that can be specified to fetch the next page. Treat the continuation token as an opaque value. Pass it back unchanged, together with the same query, parameters and `sessionId`.

The optional `sessionId` limits how much storage paging uses on the ADX cluster. Calls with the same `sessionId` and query type share one stored query result. A new first page, for example after a filter change, replaces the stored result instead of adding another one. A good value is a GUID generated by the front end for each user session, or for each browser tab. Without a `sessionId`, every first page creates a separate stored result.

#### Paging security model

Holding a continuation token or `sessionId` gives no access beyond what sending the same request would already return:

* The stored query result name is computed on the server from the query type and a hash of the `sessionId`, or of a random nonce. The name is never taken from the continuation token.
* Each stored row is tagged with a fingerprint of the query type, query text and parameters. Later pages only return rows whose fingerprint matches the current request. A token can't be replayed against another query type, other filters or another tenant.

> [!IMPORTANT]
> Any value that scopes the data, such as a tenant id or user id, must be a parameter on the query record or part of the query text. Only then is it included in the fingerprint.

#### Expiration and resuming

Stored query results expire after `CabazureKustoOptions.PagedResultExpiration`, which defaults to 30 minutes. The maximum is 24 hours, the ADX limit. You can set it per named connection:

```csharp
builder.Services.AddCabazureKusto(kusto => kusto
  .Configure(o =>
  {
    o.WithHostAddress("https://help.kusto.windows.net/");
    o.PagedResultExpiration = TimeSpan.FromMinutes(15);
  }));
```

A continuation token can still be used after its stored result has expired, or after another request with different filters has replaced it. The query is re-run into the same stored result, and the requested page is returned from that new snapshot, so a user can pick up where they left off. If the data changed in the meantime, rows may shift. When a total count was requested, it is refreshed at the same time.

#### Total count

To also get the total number of items across all pages, use the overload that takes an `includeTotalCount` flag:

```csharp
var result = await processor.ExecuteAsync(
  new CustomerQuery("type"),
  sessionId,
  maxItemCount,
  continuationToken,
  includeTotalCount: true,
  cancellationToken);

long? total = result?.TotalCount;
```

The total count is opt-in. When you don't request it, `TotalCount` is `null` and paging works exactly as before, with no extra cost.

When you request it, the total is fetched once on the first page by running `.show stored_query_results`. This call reads the stored result's metadata and doesn't rerun your query. It is skipped when the first page isn't full, because the item count is then the total. The total is included in the continuation token, so later pages need no extra calls. Paging also stops as soon as the total is reached.

### Streaming queries

When you want to process large result sets row-by-row without materializing the full result into memory first, derive your query from `StreamKustoQuery<T>` and execute it with `ExecuteAsync()`.

```csharp
public record CustomerExportQuery(string CustomerType)
  : StreamKustoQuery<Customer>;

app.MapGet(
  "/customers/export",
  (IKustoProcessor processor, CancellationToken cancellationToken)
    => processor.ExecuteAsync(
      new CustomerExportQuery("type"),
      cancellationToken));
```

Streaming queries reuse the same row deserialization rules as `KustoQuery<T>`, including support for dynamic columns, `SqlDecimal`, `DBNull`, and `DateOnly`. Row-by-row delivery is driven by the underlying Kusto SDK's HTTP response streaming (enabled by default via `KustoConnectionStringBuilder.Streaming`), so rows are exposed lazily to the caller without buffering the full result set client-side.

## Ingestion

Install the optional ingestion package when an application needs to write typed records to Azure Data Explorer:

```powershell
dotnet add package Cabazure.Kusto.Ingest
```

Register the table and an existing JSON ingestion mapping for each record type:

```csharp
using Cabazure.Kusto.DependencyInjection;
using Cabazure.Kusto.Ingest;

builder.Services.AddCabazureKusto(kusto => kusto
  .Configure(o => o
    .WithHostAddress("https://contoso.westeurope.kusto.windows.net/")
    .WithDatabaseName("Telemetry")
    .WithCredential(new DefaultAzureCredential()))
  .AddIngestion<DataRecord>(
    tableName: "RawData",
    mappingName: "RawDataMapping"));
```

`AddIngestion<T>()` registers `IKustoIngester<T>` in dependency injection. The ingester automatically uses the configured table, mapping, connection, database, and ingestion mode:

```csharp
public sealed class DataRecordHandler(
  IKustoIngester<DataRecord> ingester)
{
  public Task<KustoIngestionResult> HandleAsync(
    IAsyncEnumerable<DataRecord> records,
    CancellationToken cancellationToken)
    => ingester.IngestAsync(
      records,
      cancellationToken);
}
```

Both `IEnumerable<T>` and `IAsyncEnumerable<T>` are supported. Records are serialized incrementally as newline-delimited JSON, so the complete input isn't buffered in memory.

`CabazureKustoOptions.SerializerOptions` is shared by typed query-result materialization and typed ingestion for the configured connection. Its defaults preserve Cabazure.Kusto's existing behavior: camel-case names, case-insensitive matching, string enums, `DateOnly`, numbers read from strings, and ignored unmapped members. Customize it with `ConfigureSerializerOptions()`:

```csharp
builder.Services.AddCabazureKusto(kusto => kusto
  .Configure(o => o
    .WithHostAddress(clusterUri)
    .WithDatabaseName(databaseName)
    .WithCredential(credential)
    .ConfigureSerializerOptions(json =>
      json.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower))
  .AddIngestion<DataRecord>("RawData", "RawDataMapping"));
```

`ConfigureSerializerOptions()` customizes Cabazure.Kusto's preconfigured instance instead of replacing it, so settings that aren't changed retain the compatibility defaults. Configure it during service registration, before the options are first used by a query or ingestion operation.

Named connections can use different serializer options. A processor or ingester created for a named connection uses that connection's `CabazureKustoOptions.SerializerOptions`. Custom `IKustoQuery<T>` and `IKustoStreamQuery<T>` implementations receive that selected instance in their required result-reading method.

Result mapping always requires the serializer options selected for the effective connection. Custom query implementations must accept and forward them:

```csharp
public record ConnectorUptimeQuery(string ChargerId)
  : KustoScript, IKustoQuery<ConnectorUptime?>
{
  public ConnectorUptime? ReadResult(
    IDataReader reader,
    JsonSerializerOptions serializerOptions)
    => reader
      .ReadObjects<ConnectorUptime>(serializerOptions)
      .FirstOrDefault();
}
```

The same applies when calling `ReadObject<T>()`, `ReadObjects<T>()`, or `ReadObjectsFromNextResult<T>()` directly. These methods no longer select global defaults because doing so could bypass the named connection's serializer configuration. Applications using the standard `KustoQuery<T>`, `StreamKustoQuery<T>`, and `IKustoProcessor` flow do not need to pass the options themselves.

When upgrading from a version with the legacy reader overloads:

- Add `JsonSerializerOptions serializerOptions` to custom `IKustoQuery<T>.ReadResult` and `IKustoStreamQuery<T>.ReadResults` implementations.
- Forward that instance to `DataReaderExtensions` or other JSON serialization operations.
- Supply serializer options when constructing processing handlers directly. All `NewStoredQueryHandler<T>` constructor arguments, including `includeTotalCount`, are now explicit.

### Ingestion modes

`AddIngestion<T>()` accepts an optional `KustoIngestionMode`. It defaults to `ManagedStreaming`.

| Mode | Completion semantics | Best suited for |
| --- | --- | --- |
| `ManagedStreaming` | Attempts streaming first and can return `Queued` when the SDK falls back to queued ingestion. | General low-latency ingestion where a resilient queued fallback is acceptable. |
| `Streaming` | A successful result means streaming ingestion completed. It fails rather than falling back to the queue. | Small payloads where low latency is required and queued fallback would be undesirable. |
| `Queued` | A successful SDK call means the source was accepted into the ingestion queue, not that ingestion completed. | Larger batches, sustained volume, and workloads prioritizing throughput and reliability. |

Select a different default mode when registering a type:

```csharp
kusto.AddIngestion<DataRecord>(
  tableName: "RawData",
  mappingName: "RawDataMapping",
  mode: KustoIngestionMode.Queued);
```

Managed streaming automatically retries or moves suitable failures to queued ingestion. Consequently, inspect `KustoIngestionResult.Method` and `KustoIngestionResult.Status`: a managed request can return `Streaming`/`Succeeded` or `Queued`/`Queued`.

Streaming ingestion must be enabled on the target table or database. Strict streaming supports only precreated ingestion mappings. Queued ingestion is generally preferable for high sustained volume into an individual table; Azure Data Explorer guidance recommends considering queued ingestion above approximately 4 GB per hour per table.

### Overriding execution scope

Use `IKustoIngesterFactory` when an individual operation should override the registered connection, database, or ingestion mode. The table and mapping remain associated with `T`:

```csharp
IKustoIngester<DataRecord> ingester = factory.Create<DataRecord>(
  connectionName: "archive",
  databaseName: "HistoricalTelemetry",
  mode: KustoIngestionMode.Queued);
```

Only one ingestion destination can be registered for a given .NET type. Use a distinct record type when the same data shape must represent a different table or mapping.

The Kusto Ingest SDK doesn't accept a cancellation token for an ingestion request. Cancellation stops Cabazure's enumeration and serialization pipeline, but a request already issued to the SDK might finish through stream termination rather than cooperative service cancellation.

## Sample

Please see the [SampleApi project](https://github.com/Cabazure/Cabazure.Kusto/tree/main/samples/SampleApi), for an example of how Cabazure.Kusto can be setup to query the "ContosoSales" database of the ADX sample cluster.
