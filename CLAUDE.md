# Finch — Lending Platform Tech Test

Reference material for AI-assisted development on this repo. This is a **timeboxed (1h) technical
test** for Blackfinch (see [`docs/spec.pdf`](docs/spec.pdf)) — not production code. It simulates a
basic lending platform: a console app takes loan applications, applies business rules to
approve/decline them, and reports running statistics.

This file exists to (a) give any AI assistant working on this repo full context without
re-deriving it, and (b) satisfy the test's requirement to log AI prompts, iterations, and
corrections.

## Source of truth

If anything below conflicts with the scaffolded project files (`src/Finch.Console/`,
`src/Finch.Console.UnitTests/`, `src/Finch.Console.IntegrationTests/`) or with
[`docs/spec.pdf`](docs/spec.pdf), **the scaffolded code and the spec win**. This document is a
plan/record, not the contract.

## Domain summary

**Inputs** (per application): loan amount (GBP), asset value the loan is secured against (GBP),
applicant credit score (1–999).

**LTV** (loan-to-value) = `(LoanAmount / AssetValue) * 100`.

**Outputs** (console): per-application decision (approved / declined + reason), running totals —
applicant count by outcome, total value of *approved* loans, and mean LTV across **all**
applications (approved and declined alike).

### Business rules (flattened)

The spec expresses the low-value rules as cascading "if LTV < X" thresholds. These have been
flattened into independent, mutually-exclusive LTV bands (confirmed as correct by Craig — the
alternative, layering all three conditions when LTV is low, doesn't make sense with a single
credit score requirement per band). See **Future considerations** below for the risk this
flattening introduces.

| # | Applies when | Rule | Spec source |
|---|---|---|---|
| 1 | always | `LoanAmount >= 100,000` | General limit |
| 2 | always | `LoanAmount <= 1,500,000` | General limit |
| 3 | `LoanAmount >= 1,000,000` | `LTV <= 60` | High value |
| 4 | `LoanAmount >= 1,000,000` | `CreditScore >= 950` | High value |
| 5 | `LoanAmount < 1,000,000` | `LTV < 90` | Low value cap (LTV ≥ 90 ⇒ decline) |
| 6 | `LoanAmount < 1,000,000 && LTV < 60` | `CreditScore >= 750` | Low value band 1 |
| 7 | `LoanAmount < 1,000,000 && 60 <= LTV < 80` | `CreditScore >= 800` | Low value band 2 |
| 8 | `LoanAmount < 1,000,000 && 80 <= LTV < 90` | `CreditScore >= 900` | Low value band 3 |

An application is **approved** only if every applicable rule is satisfied. On decline, the reason
shown is the **first** rule (in evaluation order) that failed — see Future considerations.

## Architecture

- **Domain records**: `LoanApplicationRequest` (raw, pre-validated user input) →
  `LoanApplication` (domain object with a computed `LoanToValue` property). Kept as two distinct
  types so the domain object can assume valid, non-zero `AssetValue` (no div-by-zero guard needed
  once you're holding a `LoanApplication`).
- **Field-level validation, not a batch validator**: each input field is validated (and
  re-prompted on failure) at the point of console input, one field at a time, rather than
  constructing a whole `LoanApplicationRequest` and validating it afterwards with a dictionary of
  errors. See **AI collaboration log** — this replaced an earlier design.
- **Specification pattern**: `ISpecification<LoanApplication>` with `IsApplicableTo`,
  `IsSatisfiedBy`, `Description`, and `Order`. Each of the 8 rules above is its own class — no
  combined/compound rules. All implementations are registered in DI and injected into a
  `RulesEngine` as `IEnumerable<ISpecification<LoanApplication>>`; the engine sorts by `Order`
  (matching the table's numbering, 1–8) before evaluating, so evaluation order — and therefore
  which reason is returned on decline — is a stable, explicit contract rather than an accident of
  DI registration order.
- **Single console writer**: exactly one class is allowed to call `System.Console.Write*` — an
  `IConsoleWriter` implementation, injected everywhere output happens. This is what integration
  tests swap out (analogous to `FakeLogger` for `ILogger`) to assert on printed output without
  touching the real console. `System.Console.ReadLine` (input) is not subject to this constraint —
  it isn't part of what tests assert on, so the interactive reader calls it directly.
  - `ILogger` was deliberately **not** used for this output — routing decision/stats output through
    a logging abstraction would muddy the console output with log-level formatting, and there's no
    other sink to justify it right now.
- **DI container**: `Microsoft.Extensions.DependencyInjection`. `Program.cs` is the composition
  root. Integration tests build the same container (the real rule collection, resolved for real)
  and substitute only the `IConsoleWriter`.
- **Synchronous only**: nothing here needs `async`/`await` — no I/O beyond console and no
  persistence. See Future considerations.

## Future considerations (out of scope for the 1h timebox)

- **Rule grouping/relationships**: the 8 rules are currently flat and independent. The spec's
  cascading structure hints at families (general limits vs. high-value vs. low-value-by-band) that
  could become explicit groupings later — e.g. if new loan categories are added, or if rules need
  shared metadata (a category tag, a severity, an owner).
- **Only the first failure is surfaced.** Because decline reasons are "first failing rule in
  evaluation order," fixing one problem in a UI/API consumer could just reveal the next one
  ("whack-a-mole"). A production version should probably surface *all* failing rules at once.
  Evaluation order itself is no longer the risk here - each `ISpecification<T>` declares an
  explicit `Order`, and `RulesEngine` sorts by it before evaluating, so which reason comes back
  first is a stable, tested contract independent of DI registration order (see AI log below).
- **Synchronous rule evaluation**: fine today since there's no I/O per rule. If rules ever need to
  call out to a credit bureau, fraud service, etc., `ISpecification` and `RulesEngine` would need
  async variants.
- **Rules persistence & versioning**: rule thresholds (£100k/£1.5m/£1m, 60/80/90% LTV bands, credit
  score cutoffs) are hardcoded in each specification class. For audit purposes (proving which rule
  version applied to a historical decision), these would need externalising — e.g. a rules table
  with effective-dated versions — rather than living in source code.
- **Logging & traceability**: no logging is implemented. A production system would need an audit
  trail of every decision (inputs, which rules ran, which passed/failed, final outcome) independent
  of the console output shown to the user.
- **Console UX for invalid input**: invalid input triggers an immediate re-prompt of just that
  field, in a loop, with no way to cancel an in-progress application short of killing the process.
  This is an accepted limitation of a plain console app without a TUI library.

## AI collaboration log

### Initial brief → clarifying questions

Craig described the console app, business rules, and several early design decisions already made
(the writer-isolation constraint, the specification pattern, the exact 8 rules, DI-injected
`IEnumerable<ISpecification<LoanApplication>>`) and asked for clarifying questions before any
implementation plan was written. Four genuinely open questions were asked (rather than guessed at)
because they materially shape `Program.cs` and the DI composition:

1. **How should the app handle multiple applications in one run**, given the output requires
   aggregate stats across "all applications"?
   → **Answer**: loop interactively until the user quits; print the summary after every
   application (not batched, not fixed-count).

2. **What happens on invalid input** (negative amount, credit score outside 1–999, non-numeric)?
   → **Answer**: this prompted Craig to revise his own earlier sketch. The original plan was a
   whole-object `LoanApplicationRequestValidator.Validate(request) -> ValidationResult` returning a
   `Dictionary<string, string>` of errors (FluentValidation-shaped, without the library). Craig
   reconsidered live: **validate each field as it's entered, and re-prompt just that field until
   it's valid** — no batch validation step, no error dictionary, no "declined for validation
   reasons" bucket. Explicitly flagged as a UX compromise acceptable for a plain console app
   (no TUI library in scope).

3. **Should stats print after every application or only at the end?** → **Answer**: after every
   application (confirms #1).

4. **Currency/percentage formatting?** → **Answer**: sensible defaults — £ with thousands
   separators and 2dp for money, LTV to 2dp.

### Implementation pass

Built per [`docs/implementation-plan.md`](docs/implementation-plan.md) once Craig confirmed it,
file-by-file, then verified rather than assumed working:

- Ran `dotnet build`, the interactive app (via piped stdin covering an approval, a general-limit
  decline, and several invalid-input retries across all three fields), and the full test suite
  after every meaningful chunk of work — not just at the end.
- **`dotnet test` failed immediately** with `xunit.v3`/`Microsoft.NET.Test.Sdk` on the .NET 10 SDK:
  "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK
  and later." This wasn't something to guess around — investigated rather than downgrading
  packages or reaching for `--no-verify`-style shortcuts. Root cause: xUnit v3 runs on the newer
  Microsoft.Testing.Platform (MTP), not VSTest, and .NET 10's `dotnet test` needs an explicit
  opt-in. Fixed by adding a [`global.json`](global.json) at the repo root
  (`{ "test": { "runner": "Microsoft.Testing.Platform" } }`) and switching invocations from
  positional paths to `dotnet test --solution src/Finch.sln` (the new CLI's required syntax).
  Confirmed by re-running the full suite (79 tests) after the fix, not just reading that it looked
  right.
- This same issue would have silently broken **CI** — `.github/workflows/build-and-test.yml` ran
  `dotnet test ./Finch.sln --configuration Release --collect "XPlat Code Coverage"` (positional
  path, VSTest's `--collect` coverage flag). It also had an unrelated pre-existing bug: the build
  step's `working-directory: src/api` doesn't exist (the solution lives in `src/`). Both were fixed
  ­- the working directory corrected, and the test step updated to `dotnet test --solution
  ./Finch.sln --configuration Release`. Coverage collection (`coverlet.collector` +
  `reportgenerator`) was **removed** rather than patched: `coverlet.collector` integrates via
  VSTest's data-collector mechanism, which the MTP runner doesn't support - properly restoring
  coverage would mean adding `Microsoft.Testing.Extensions.CodeCoverage` and re-plumbing the
  report-generation step, which is out of scope for a 1-hour tech test's CI polish. Flagged here
  rather than silently dropped.
- `dotnet format --verify-no-changes` (also CI-enforced, in `dotnet-linting.yml`) initially failed
  across every new file on two rules from this repo's `.editorconfig`: `insert_final_newline =
  false` (every new file had a trailing newline) and C# import ordering (`System.*` usings must
  sort with the rest, not last). Fixed by running `dotnet format` and re-verifying, rather than
  hand-editing each file.
- One early implementation mistake caught before it shipped: the first draft of
  `ConsoleLoanApplicationReader` used a generic `FieldParseResult<TValue>` record struct referenced
  as a bare, non-generic `FieldParseResult.Ok(value)` - which doesn't compile in C# (a generic
  type's static members need type arguments; there's no bare-name inference the way there is for a
  method-level generic like `Tuple.Create`). Simplified to two non-generic methods
  (`ReadDecimalField`/`ReadIntField`) instead of chasing the generic-inference pattern further,
  since there are only ever three fields to read.

### Test file convention (retrofit)

After the first implementation pass (all 79 tests passing, flat `ClassNameTests` files with plain
`[Fact]`/`[Theory]` methods), Craig specified a house convention that should have been given up
front: each test class is `static class ClassNameTests`, with an optional nested
`abstract class ClassNameTestsBase` for shared setup, and one nested `class MethodName : ...Base`
per method under test, containing `Should<Description>_When<Condition>` methods. Every test file
was rewritten to this shape (nested `IsApplicableTo`/`IsSatisfiedBy`/`Evaluate`/`Record` classes,
etc.), rebuilt, reformatted, and re-run - same 79 tests, same coverage, new shape.

One deliberate deviation, called out rather than silently "matched": the integration tests' base
class (`LendingPlatformIntegrationTestsBase`) needs real setup logic (building the DI container,
resolving services) that doesn't fit as a primary-constructor one-liner the way the unit test bases
do (which just need `new()` for a specification/validator). It uses a normal constructor body
instead of the `ClassNameTestsBase()` primary-constructor shape used everywhere else.

### Change request: explicit rule ordering

Craig asked for an `Order` property on the specifications, sorted before evaluation, "to ensure
consistency across calls" - directly closing the DI-registration-order risk flagged in Future
considerations above (and in the original implementation plan's assumptions). Added `int Order`
to `ISpecification<T>`, assigned 1–8 to the existing rules matching the table's numbering, and
changed `RulesEngine` to `OrderBy(specification => specification.Order)` in its constructor rather
than trusting enumeration order. `AddLendingPlatform`'s registration order is now cosmetic only -
its doc comment was updated to say so.

Added a unit test (`ShouldEvaluateByOrderProperty_RegardlessOfTheOrderRulesWereSupplied`) that
constructs fake specifications deliberately out of `Order` sequence and asserts the engine still
evaluates - and stops - by `Order`, not by the sequence they were passed in. Without this test, a
future refactor could reintroduce an implicit ordering dependency without anything failing.

### Change request: upper bound on loan amount and asset value

Craig asked for an upper bound on both fields "to protect against overflow," suggesting 1
quadrillion minus 0.01 (`999,999,999,999,999.99`). Added that constant to
`LoanApplicationFieldValidator` and a new switch-expression branch on both `ValidateLoanAmount` and
`ValidateAssetValue`. Worth noting for context: `decimal`'s actual range is far larger
(~7.9×10^28), so this isn't preventing a real arithmetic overflow in `LoanApplication.LoanToValue`
- it's a defensive sanity bound on unrealistic input, requested and implemented as specified rather
than second-guessed.

Boundary tests for this were written as plain `[Fact]`s with in-source `decimal` literals
(`999_999_999_999_999.99m`), not `[InlineData]`. `[InlineData]` numeric literals are compiled as
`double` and converted to `decimal` at test-invocation time; at this magnitude (17 significant
digits, beyond `double`'s ~15-17 digit precision) that round-trip risked landing a hair off the
intended boundary and making the test flaky in a way that wouldn't be obvious from reading it.

This log will be extended as implementation proceeds — further iterations, corrections, or
questioned AI output belong here, per the test's requirement to document AI usage.
