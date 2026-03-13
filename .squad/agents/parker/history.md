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
- The Cabazure.Kusto test project migrated from `Atc.Test` to `Cabazure.Test` with a csproj-only change because the existing theory attributes already match Cabazure.Test's root namespace.
- `dotnet test .\Cabazure.Kusto.sln --no-restore --verbosity minimal` is a fast, sufficient validation path for this repo after test-infrastructure changes.

## Recent Work (2026-03-13)

Completed Cabazure.Test migration on feature branch `feature/migrate-cabazure-test`:
- Swapped test package/global using from `Atc.Test` to `Cabazure.Test` in csproj
- No source-level test changes required; existing attributes resolved correctly
- All 73 tests passed validation
- Created focused commits: `6bfca2a` (migration), `0aa14f5` (validation)
- Ready for team review and merge
