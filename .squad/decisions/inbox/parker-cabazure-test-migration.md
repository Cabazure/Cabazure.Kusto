### 2026-03-13: Cabazure.Test migration shape
**By:** Parker
**What:** Migrate `test\Cabazure.Kusto.Tests` by swapping the test package/global using from `Atc.Test` to `Cabazure.Test` and keep the existing test source unchanged unless compilation proves otherwise.
**Why:** Cabazure.Test exposes the same `AutoNSubstituteData` and related theory attributes from the root `Cabazure.Test` namespace, so this repo's tests do not need source-level rewrites for the migration.
