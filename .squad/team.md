# Squad Team

> Cabazure.Kusto

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Ripley | Lead | `.squad/agents/ripley/charter.md` | ✅ Active |
| Parker | Core Dev | `.squad/agents/parker/charter.md` | ✅ Active |
| Ash | ADX/Kusto Specialist | `.squad/agents/ash/charter.md` | ✅ Active |
| Lambert | Tester | `.squad/agents/lambert/charter.md` | ✅ Active |
| Dallas | Release Engineer | `.squad/agents/dallas/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | — | 🔄 Monitor |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Small bug fixes with clear reproduction steps
- Adding or updating tests with clear acceptance criteria
- Dependency updates and version bumps
- README and sample cleanup
- Small follow-the-pattern features inside existing library surfaces

**🟡 Needs review — route to @copilot but require squad review:**
- Medium refactors with existing tests
- New handlers or helpers that follow established patterns
- Sample API improvements with explicit requirements

**🔴 Not suitable — route to squad members instead:**
- Public API design and architecture decisions
- Security-sensitive authentication or credential work
- Pagination protocol changes and continuation token semantics
- ADX/Kusto behavior changes needing domain judgment
- Performance-critical changes requiring benchmarking

## Project Context

- **Owner:** Ricky Kaare Engelharth
- **Stack:** C# / .NET 10, Azure Data Explorer (Kusto), xUnit v3, AutoFixture, NSubstitute, FluentAssertions, GitHub Actions, NuGet
- **Description:** A .NET library that extends the official ADX/Kusto SDK with embedded `.kusto` scripts, request parameter handling, typed result mapping, and pagination support.
- **Project:** Cabazure.Kusto
- **Created:** 2026-03-13
