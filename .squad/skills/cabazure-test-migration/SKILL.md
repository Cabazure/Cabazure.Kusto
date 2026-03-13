---
name: "cabazure-test-migration"
description: "Heuristics for migrating xUnit test projects from Atc.Test to Cabazure.Test"
domain: "testing"
confidence: "high"
source: "lambert"
---

## Context

Use this when a Cabazure repository migrates tests from `Atc.Test` to `Cabazure.Test`. The migration guide lives in the sibling library as `MIGRATING.md`.

## Patterns

### Start with a mechanical sweep

First replace the NuGet package and the global using:

- `<PackageReference Include="Atc.Test" .../>` → `<PackageReference Include="Cabazure.Test" .../>`
- `<Using Include="Atc.Test" />` → `<Using Include="Cabazure.Test" />`

In many repos this is the whole code change if the tests only use fixture/data attributes.

### Search for semantic migration triggers

Before trusting a mechanical swap, search for these Atc.Test-specific APIs from the migration guide:

- `WaitForCall`
- `WaitForCallForAnyArgs`
- `ReceivedCallWithArgument`
- `CompareJsonElementUsingJson`
- `InvokeProtectedMethod`
- `AddTimeout`
- `HasProperties`
- `AutoRegister`
- `JsonElementCustomization`

If none are present, the migration risk is mostly compile-time namespace drift.

### Review fixture-driven tests for left-to-right dependency resolution

`Cabazure.Test` still resolves theory parameters left to right. Preserve `[Frozen]` parameters before the SUT or any dependent parameter.

### Re-test edge-heavy suites first

In Cabazure-style libraries, the first regression targets after migration are:

- serialization/deserialization tests
- pagination/continuation-token tests
- request-property mapping tests

These are the places where fixture or assertion plumbing changes tend to surface indirectly.

## Anti-Patterns

- **Rewriting test behavior during the package swap** — keep the migration narrow unless compile failures force a semantic change.
- **Ignoring renamed helper searches** — missing one Atc.Test helper can turn a “simple package swap” into a broken test project.
