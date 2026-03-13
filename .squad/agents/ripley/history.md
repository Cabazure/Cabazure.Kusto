# Project Context

- **Project:** Cabazure.Kusto
- **Owner:** Ricky Kaare Engelharth
- **Stack:** C# / .NET 10, Azure Data Explorer (Kusto), xUnit v3, AutoFixture, NSubstitute, FluentAssertions, GitHub Actions, NuGet
- **Description:** A .NET library that extends the ADX/Kusto SDK with embedded scripts, request parameter handling, typed result mapping, and pagination support.
- **Created:** 2026-03-13

## Core Context

Ripley leads cross-cutting work and reviewer gates for the library.

## Learnings

- The library centers on `KustoScript`, `KustoQuery<T>`, `KustoCommand`, `IKustoProcessor`, and script handlers.
- Pagination behavior is an important compatibility surface and should be treated as a first-class review concern.
