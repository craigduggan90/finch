# Finch — Lending Platform (tech test)

A console app simulating a basic lending platform, built for the Blackfinch backend engineering
tech test (see [`docs/spec.pdf`](docs/spec.pdf)). Not production-ready — see
[`CLAUDE.md`](CLAUDE.md) for the architecture write-up, business rules, design decisions, and the
AI collaboration log.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Running the app

```bash
dotnet run --project src/Finch.Console
```

You'll be prompted for each application's **loan amount (GBP)**, **asset value (GBP)**, and
**applicant credit score (1–999)**, one field at a time — an invalid value re-prompts just that
field. After each application, the decision (approved, or declined with a reason) and the running
totals (applicant counts, total value of loans written, mean LTV across all applications) are
printed. You'll then be asked whether to submit another application; anything other than `y`/`yes`
ends the session. You can also end the session at any prompt by sending EOF - `Ctrl+D` on
macOS/Linux, `Ctrl+Z` then `Enter` on Windows - which prints "No more input received - ending
session." and exits cleanly rather than re-prompting forever.

## Running the tests

```bash
dotnet test --solution src/Finch.sln
```

This runs both `Finch.Console.UnitTests` (clear-box, boundary-exhaustive tests of every business
rule in isolation) and `Finch.Console.IntegrationTests` (pseudo end-to-end tests against the real
dependency-injection-resolved rule set, with only the console output substituted for an in-memory
fake).

> Note: this repo uses xUnit v3, which runs on the newer Microsoft.Testing.Platform rather than
> VSTest. `dotnet test` requires `--solution`/`--project` instead of a positional path, and the
> [`global.json`](global.json) at the repo root opts the SDK into that runner - both are required
> for `dotnet test` to work under the .NET 10 SDK.

## Business rules

See [`CLAUDE.md`](CLAUDE.md#business-rules-flattened) for the full rule table and the reasoning
behind flattening the spec's cascading LTV thresholds into independent bands.

## AI usage

This project was built with Claude Code. The AI collaboration log — key prompts, the clarifying
questions asked before implementation, and a design decision that was reconsidered mid-conversation
— is in [`CLAUDE.md`](CLAUDE.md#ai-collaboration-log).
