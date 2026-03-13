# Project Context

- **Project:** Cabazure.Kusto
- **Owner:** Ricky Kaare Engelharth
- **Stack:** C# / .NET 10, Azure Data Explorer (Kusto), xUnit v3, AutoFixture, NSubstitute, FluentAssertions, GitHub Actions, NuGet
- **Description:** A .NET library that extends the ADX/Kusto SDK with embedded scripts, request parameter handling, typed result mapping, and pagination support.
- **Created:** 2026-03-13

## Core Context

Lambert owns testing, regression analysis, and reviewer gates focused on quality.

## Learnings

- The test stack uses xUnit v3, AutoFixture, NSubstitute, FluentAssertions, and Cabazure.Test (migrated from Atc.Test).
- High-value regression areas include `DataReaderExtensions`, `KustoScriptExtensions`, and the stored query handlers.
- The sibling `Cabazure.Test` repo ships its migration guide as `MIGRATING.md`, not `MIGRATION.md`.
- `Cabazure.Kusto.Tests` does not use the risky Atc.Test-only APIs from the migration guide (`WaitForCall*`, `ReceivedCallWithArgument`, `AddTimeout`, `HasProperties`, `AutoRegister`, or `JsonElementCustomization`), so this migration stays mostly mechanical.
- Infrastructure migration validated via full `dotnet test .\Cabazure.Kusto.sln --no-restore --verbosity minimal` suite; all 73 tests passing confirms no hidden breaks in namespace/package resolution for `FixtureFactory`, `AutoNSubstituteData`, and `MemberAutoNSubstituteData`.

## Recent Work (2026-03-13)

Completed risk analysis and review gates for Cabazure.Test migration:
- Scanned all test files for risky Atc.Test-only APIs; **none found**
- Classified as low-risk infrastructure-only change
- Documented regression-prone areas and validation approach
- Approved Parker's migration work; ready for team consensus and merge
