# Project Context

- **Project:** Cabazure.Kusto
- **Owner:** Ricky Kaare Engelharth
- **Stack:** C# / .NET 10, Azure Data Explorer (Kusto), xUnit v3, AutoFixture, NSubstitute, FluentAssertions, GitHub Actions, NuGet
- **Description:** A .NET library that extends the ADX/Kusto SDK with embedded scripts, request parameter handling, typed result mapping, and pagination support.
- **Created:** 2026-03-13

## Core Context

Lambert owns testing, regression analysis, and reviewer gates focused on quality.

## Learnings

- The test stack uses xUnit v3, AutoFixture, NSubstitute, FluentAssertions, and Atc.Test.
- High-value regression areas include `DataReaderExtensions`, `KustoScriptExtensions`, and the stored query handlers.
