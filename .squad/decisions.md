# Squad Decisions

## Active Decisions

### 2026-03-13: Initial squad specialization
**By:** Squad (Coordinator)
**What:** The Cabazure.Kusto squad is specialized into five active members: Ripley (lead/reviewer), Parker (core C# library), Ash (ADX/Kusto semantics), Lambert (testing), and Dallas (release/docs/sample flow). Scribe remains the silent memory manager and Ralph remains the work monitor.
**Why:** This repository is a .NET library with three dominant maintenance surfaces: core library code, Kusto-specific behavior, and quality/release workflow. A specialized team reduces routing ambiguity and keeps public API, query behavior, and test coverage aligned.

### 2026-03-13: Cabazure.Test migration shape
**By:** Parker
**What:** Migrate `test\Cabazure.Kusto.Tests` by swapping the test package/global using from `Atc.Test` to `Cabazure.Test` and keep the existing test source unchanged unless compilation proves otherwise.
**Why:** Cabazure.Test exposes the same `AutoNSubstituteData` and related theory attributes from the root `Cabazure.Test` namespace, so this repo's tests do not need source-level rewrites for the migration.

### 2026-03-13: Cabazure.Test migration risk assessment
**By:** Lambert
**What:** Classify the Cabazure.Kusto test migration as a **test-infrastructure swap**, not a semantic test rewrite. No usage of risky Atc.Test-only APIs (`WaitForCall*`, `ReceivedCallWithArgument`, `CompareJsonElementUsingJson`, `InvokeProtectedMethod`, `AddTimeout`, `HasProperties`, `[AutoRegister]`, `JsonElementCustomization`) was found in the test codebase.
**Why:** The migration risk is limited to compile-time namespace and transitive-package resolution. Existing test assertions and patterns require no rewrites. Regression validation focuses on `DataReaderExtensions`, `KustoScriptExtensions`, and stored query handlers; all passed with existing 73-test suite.

### 2026-03-13: User directive — feature branch and focused commits
**By:** Ricky Kaare Engelharth (via Copilot)
**What:** Perform the Cabazure.Test migration work on a feature branch with focused, atomic commits.
**Why:** User-directed governance for this migration work. Captured for team memory and decision audit trail.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
