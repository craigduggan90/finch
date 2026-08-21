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
  `IsSatisfiedBy`, and `Description`. Each of the 8 rules above is its own class — no combined/
  compound rules. All implementations are registered in DI and injected into a `RulesEngine` as
  `IEnumerable<ISpecification<LoanApplication>>`.
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
- **Execution order matters, and only the first failure is surfaced.** Because decline reasons are
  "first failing rule in evaluation order," fixing one problem in a UI/API consumer could just
  reveal the next one ("whack-a-mole"). A production version should probably surface *all* failing
  rules at once, or make evaluation order an explicit, tested contract rather than an implicit
  consequence of DI registration order.
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

This log will be extended as implementation proceeds — further iterations, corrections, or
questioned AI output belong here, per the test's requirement to document AI usage.
