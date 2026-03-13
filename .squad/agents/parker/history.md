# Project Context

- **Project:** Cabazure.Kusto
- **Owner:** Ricky Kaare Engelharth
- **Stack:** C# / .NET 10, Azure Data Explorer (Kusto), xUnit v3, AutoFixture, NSubstitute, FluentAssertions, GitHub Actions, NuGet
- **Description:** A .NET library that extends the ADX/Kusto SDK with embedded scripts, request parameter handling, typed result mapping, and pagination support.
- **Created:** 2026-03-13

## Core Context

Parker owns core implementation work in the library.

## Learnings

- `KustoProcessor`, `KustoProcessorFactory`, `KustoClientProvider`, and the processing handlers are central implementation surfaces.
- Changes should follow the repository's small-type, file-scoped namespace, explicit-type style.
