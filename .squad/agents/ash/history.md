# Project Context

- **Project:** Cabazure.Kusto
- **Owner:** Ricky Kaare Engelharth
- **Stack:** C# / .NET 10, Azure Data Explorer (Kusto), xUnit v3, AutoFixture, NSubstitute, FluentAssertions, GitHub Actions, NuGet
- **Description:** A .NET library that extends the ADX/Kusto SDK with embedded scripts, request parameter handling, typed result mapping, and pagination support.
- **Created:** 2026-03-13

## Core Context

Ash owns Kusto semantics, embedded script behavior, and pagination compatibility.

## Learnings

- `KustoScript` loads `{FullTypeName}.kusto` embedded resources and camel-cases public property names for parameters.
- `KustoScriptExtensions`, `NewStoredQueryHandler<T>`, and `ExistingStoredQueryHandler<T>` define critical protocol behavior.
