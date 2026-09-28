# Copilot instructions for Cabazure.Kusto

## Repository purpose

This repository contains `Cabazure.Kusto`, a .NET library that extends the official Azure Data Explorer / Kusto SDK.

Primary responsibilities:

- Load embedded `.kusto` scripts from .NET types
- Convert script properties into Kusto request parameters
- Deserialize `IDataReader` results into typed contracts
- Support pagination through stored query results and continuation tokens
- Provide dependency injection registration and processor factory abstractions

## Core architecture

- `KustoScript` is the root abstraction. It loads an embedded resource named `{FullTypeName}.kusto` from the same assembly and extracts public property values as parameters.
- `KustoCommand` represents scripts with no typed output.
- `KustoQuery<T>` represents scripts with typed output. The default implementation reads objects from `IDataReader` through `DataReaderExtensions`.
- `KustoScriptExtensions` converts script parameters into `ClientRequestProperties` and CSL literals.
- `IKustoProcessor` / `KustoProcessor` are the public execution entry points.
- `IScriptHandlerFactory` selects the execution strategy:
  - `SimpleCommandHandler` for commands
  - `SimpleQueryHandler<T>` for normal queries
  - `NewStoredQueryHandler<T>` for the first paged request
  - `ExistingStoredQueryHandler<T>` for subsequent paged requests
- `KustoClientProvider` owns and caches query/admin clients per connection and database.
- `CabazureKustoBuilder` configures named connections and is extended by the optional `Cabazure.Kusto.Ingest` package.
- `IKustoIngester<T>` incrementally serializes typed records as JSON lines into a temporary seekable file before passing it to the Kusto Ingest SDK.
- `IKustoIngesterFactory` overrides connection, database, or ingestion mode while reusing the destination registered for `T`.

## Expectations for new code

- Preserve the public model built around `KustoScript`, `KustoCommand`, `KustoQuery<T>`, `IKustoProcessor`, and `IKustoProcessorFactory`.
- Prefer adding behavior by extending the existing processing pipeline instead of bypassing it.
- Reuse existing helpers for parameter conversion, request property creation, and `IDataReader` deserialization.
- Keep changes small and composable. Favor new handlers or focused helpers over pushing unrelated logic into `KustoProcessor`.

## Embedded script conventions

- When adding a new query or command, pair the .NET type with an embedded `.kusto` file that uses the same namespace and type name.
- The `.kusto` file must be added as an `EmbeddedResource`.
- Script parameters come from the public properties of the record/class. Property names are camel-cased before being sent to Kusto.
- Prefer record types for queries and commands, especially when parameters are part of the type shape.

## Serialization and contracts

- Query result contracts should match the projected Kusto column names after camel-case normalization.
- Query result deserialization and typed ingestion share `CabazureKustoOptions.SerializerOptions` for the effective named connection.
- `CabazureKustoOptions` owns the shared serializer defaults. Customize its getter-only options instance through `ConfigureSerializerOptions(...)`; do not replace it or create separate query and ingestion settings.
- Query interfaces, handlers, and `DataReaderExtensions` require an explicit non-null `JsonSerializerOptions`; do not add global defaults, nullable fallbacks, or compatibility overloads that can bypass named configuration.
- Preserve the JSON-based mapping approach and its seeded defaults unless there is a strong reason to change them. In particular, unmapped-member skipping is required for stored-query paging columns.
- Be careful with special handling already present for enums, `DateOnly`, `DateTimeOffset`, `SqlDecimal`, `JToken`, and nullable values.
- If you change result mapping behavior, add or update tests that cover the conversion path from `IDataReader` to the contract type.

## Pagination behavior

- Keep paged-query behavior compatible with the stored query result flow already implemented in `NewStoredQueryHandler<T>` and `ExistingStoredQueryHandler<T>`.
- Preserve continuation token semantics. The current format is `{queryId};{itemsReturned}`.
- Keep query id sanitization and request property propagation intact.
- Do not silently change preview count, expiration, row numbering, or continuation token behavior without updating tests and docs.

## Dependency injection and configuration

- Register services through `AddCabazureKusto(...)` in `ServiceCollectionExtensions`.
- Add typed ingestion through `CabazureKustoBuilder.AddIngestion<T>()`; keep the Ingest SDK dependency in `Cabazure.Kusto.Ingest`.
- Keep support for both inline configuration and `IOptions`-based configuration.
- Configure named connections through `CabazureKustoBuilder.Configure(connectionName, ...)`, not through named `AddCabazureKusto` overloads.
- Keep ingestion registration connection/database independent; the injected ingester uses default options and `IKustoIngesterFactory` applies execution-scope overrides.
- Preserve named connection / named options support in `KustoClientProvider`.
- When changing configuration behavior, verify both connection-string and host-address based configuration paths.

## Coding conventions

- This is a modern C# codebase targeting `net10.0` with nullable reference types and implicit usings enabled.
- Follow `.editorconfig`: 4-space indentation in C# files, file-scoped namespaces, and explicit types are generally preferred over `var`.
- Match the existing style of small focused types, primary constructors, expression-bodied members where already used, and minimal comments.
- Avoid broad exception swallowing. If existing behavior intentionally returns `null` for invalid continuation tokens or missing stored results, preserve that behavior unless the change explicitly revisits it.

## Tests

- Add or update tests for behavior changes. This repository uses:
  - xUnit v3
  - AutoFixture / `AutoNSubstituteData`
  - NSubstitute
  - FluentAssertions
  - Atc.Test
- Follow existing test naming patterns like `ExecuteAsync_Will_Return_Result_From_Handler`.
- Favor focused unit tests around handlers, script loading, parameter conversion, and data-reader deserialization.

## Samples and documentation

- If a public API or developer workflow changes, update `README.md` and the `samples\SampleApi` project when relevant.
- Keep examples aligned with the current headers and pagination usage shown in the sample API.
