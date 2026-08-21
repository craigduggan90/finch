# Implementation Plan — Finch Lending Platform

Status: **draft — awaiting confirmation, no implementation started.**

This plan lists every file to be created/modified in `src/Finch.Console`, `src/Finch.Console.UnitTests`,
and `src/Finch.Console.IntegrationTests`, plus supporting repo files (`README.md`). See
[`CLAUDE.md`](../CLAUDE.md) for the architecture rationale and the AI collaboration log this plan
is built on.

## Assumptions carried into this plan (flag any you disagree with)

- **Path**: you asked for `/doc/implementation-plan.md`; the repo already uses `docs/` (plural, per
  `docs/spec.pdf`), so this plan lives at `docs/implementation-plan.md` instead.
- **Exit UX**: after each application's decision + stats print, the app asks
  `Submit another application? (y/n):`. Anything other than `y`/`yes` (case-insensitive) exits.
- **No new test/DI packages beyond what's needed**: `Microsoft.Extensions.DependencyInjection`
  added to `Finch.Console` (and referenced transitively / directly where the container is built in
  tests). Assertions stay plain `xUnit.Assert` — no FluentAssertions/Shouldly added, to keep the
  footprint minimal for a 1h timebox.
- **Rule evaluation order** is the registration order in the DI composition root, matching the
  order in the rules table in `CLAUDE.md`. This is an implicit ordering (see Future considerations
  in `CLAUDE.md`) — acceptable for the test, called out as a limitation.
- **Both test projects get a `ProjectReference` to `Finch.Console`**, which they currently lack.
- Decline reason text is the `Description` of the first failing applicable rule, in evaluation
  order.

## Project reference / package changes

- `Finch.Console.csproj`: add `Microsoft.Extensions.DependencyInjection`.
- `Finch.Console.UnitTests.csproj`: add `<ProjectReference Include="..\Finch.Console\Finch.Console.csproj" />`.
- `Finch.Console.IntegrationTests.csproj`: add the same `ProjectReference`, plus
  `Microsoft.Extensions.DependencyInjection` (it builds the real container).
- Remove the placeholder `UnitTest1.cs` files from both test projects once real tests exist.

## Files to create — `src/Finch.Console`

| File | Purpose |
|---|---|
| `Domain/LoanApplicationRequest.cs` | Record: raw, already-field-validated user input — `LoanAmount` (decimal), `AssetValue` (decimal), `CreditScore` (int). |
| `Domain/LoanApplication.cs` | Record: domain object mapped from the request — same three fields plus computed `LoanToValue` (`decimal`, `(LoanAmount / AssetValue) * 100`). Assumes `AssetValue > 0` (guaranteed by field validation upstream). |
| `Domain/DecisionOutcome.cs` | Enum: `Approved`, `Declined`. |
| `Domain/LoanDecision.cs` | Record: `Outcome` (`DecisionOutcome`), `Reason` (`string?`, null when approved). |
| `Validation/FieldValidationResult.cs` | Record/readonly struct: `IsValid` (bool), `ErrorMessage` (`string?`). Factory helpers `Ok()` / `Fail(message)`. |
| `Validation/LoanApplicationFieldValidator.cs` | Concrete class (no interface — single implementation, no swap requirement) with three pure methods: `ValidateLoanAmount(decimal)`, `ValidateAssetValue(decimal)`, `ValidateCreditScore(int)`. Rules: amount/value `> 0`; credit score in `[1, 999]`. Fully unit-testable in isolation, no console dependency. |
| `Rules/ISpecification.cs` | Interface as specified by Craig: `IsApplicableTo(T item)`, `IsSatisfiedBy(T item)`, `string Description`. |
| `Rules/RulesEngine.cs` | Takes `IEnumerable<ISpecification<LoanApplication>>` via constructor injection. `Evaluate(LoanApplication)` iterates in injected order, skips inapplicable rules, returns `LoanDecision` — `Declined` with the first failing rule's `Description` on first failure, else `Approved`. |
| `Rules/Specifications/MinimumLoanAmountSpecification.cs` | Applicable: always. Satisfied: `LoanAmount >= 100_000`. |
| `Rules/Specifications/MaximumLoanAmountSpecification.cs` | Applicable: always. Satisfied: `LoanAmount <= 1_500_000`. |
| `Rules/Specifications/HighValueLtvSpecification.cs` | Applicable: `LoanAmount >= 1_000_000`. Satisfied: `LoanToValue <= 60`. |
| `Rules/Specifications/HighValueCreditScoreSpecification.cs` | Applicable: `LoanAmount >= 1_000_000`. Satisfied: `CreditScore >= 950`. |
| `Rules/Specifications/LowValueMaximumLtvSpecification.cs` | Applicable: `LoanAmount < 1_000_000`. Satisfied: `LoanToValue < 90`. |
| `Rules/Specifications/LowValueLtvBand1CreditScoreSpecification.cs` | Applicable: `LoanAmount < 1_000_000 && LoanToValue < 60`. Satisfied: `CreditScore >= 750`. |
| `Rules/Specifications/LowValueLtvBand2CreditScoreSpecification.cs` | Applicable: `LoanAmount < 1_000_000 && LoanToValue >= 60 && LoanToValue < 80`. Satisfied: `CreditScore >= 800`. |
| `Rules/Specifications/LowValueLtvBand3CreditScoreSpecification.cs` | Applicable: `LoanAmount < 1_000_000 && LoanToValue >= 80 && LoanToValue < 90`. Satisfied: `CreditScore >= 900`. |
| `Statistics/LoanApplicationStatistics.cs` | Mutable, per-run aggregator (registered as a DI singleton). `Record(LoanApplication, LoanDecision)` updates: total count, approved/declined counts, sum of approved `LoanAmount` (→ total value written), running sum of `LoanToValue` across **all** applications (→ mean LTV). Exposes read-only computed properties for the presenter. |
| `Output/IConsoleWriter.cs` | Interface: `WriteLine(string message)` (and `Write(string message)` if needed for prompts without a newline). The only abstraction any other class uses to produce output. |
| `Output/SystemConsoleWriter.cs` | Sole implementation calling `System.Console.WriteLine` / `Console.Write`. |
| `Output/ApplicationResultPresenter.cs` | Formats one `LoanApplication` + `LoanDecision` + current `LoanApplicationStatistics` snapshot into human-readable lines (currency with `C2`/`N2`-style formatting, LTV to 2dp) and writes them via `IConsoleWriter`. |
| `Input/ConsoleLoanApplicationReader.cs` | Interactive reader: prompts for each field via `IConsoleWriter`, reads via `Console.ReadLine()` directly (reading is not covered by the writer-isolation rule), validates each via `LoanApplicationFieldValidator`, re-prompting the same field on failure. Returns a fully valid `LoanApplicationRequest`. Also owns the `Submit another application? (y/n)` prompt/loop-continuation check. |
| `DependencyInjection/ServiceCollectionExtensions.cs` | `AddLendingPlatform(this IServiceCollection)` — registers all 8 specifications as `ISpecification<LoanApplication>`, plus `RulesEngine`, `LoanApplicationFieldValidator`, `LoanApplicationStatistics` (singleton), `IConsoleWriter → SystemConsoleWriter`, `ApplicationResultPresenter`, `ConsoleLoanApplicationReader`. |
| `Program.cs` (replace placeholder) | Composition root: build `ServiceCollection`, call `AddLendingPlatform()`, build provider, resolve the reader/engine/statistics/presenter, run the loop: read request → map to `LoanApplication` → `RulesEngine.Evaluate` → `LoanApplicationStatistics.Record` → `ApplicationResultPresenter` prints decision + running stats → ask to continue. |

## Files to create — `src/Finch.Console.UnitTests`

Clear-box, boundary-exhaustive tests, one spec class tested in isolation per file:

| File | Covers |
|---|---|
| `Validation/LoanApplicationFieldValidatorTests.cs` | `ValidateLoanAmount`/`ValidateAssetValue` at `0`, negative, and just-above-zero; `ValidateCreditScore` at `0`, `1`, `999`, `1000`, negative. |
| `Domain/LoanApplicationTests.cs` | `LoanToValue` computation across a few representative amount/value pairs. |
| `Rules/Specifications/MinimumLoanAmountSpecificationTests.cs` | Boundary at exactly `100_000`, one below, one above. |
| `Rules/Specifications/MaximumLoanAmountSpecificationTests.cs` | Boundary at exactly `1_500_000`, one below, one above. |
| `Rules/Specifications/HighValueLtvSpecificationTests.cs` | `IsApplicableTo` boundary at `1_000_000`; `IsSatisfiedBy` boundary at `LTV == 60`. |
| `Rules/Specifications/HighValueCreditScoreSpecificationTests.cs` | Applicability boundary; satisfied boundary at `CreditScore == 950`. |
| `Rules/Specifications/LowValueMaximumLtvSpecificationTests.cs` | Applicability boundary at `< 1_000_000`; satisfied boundary at `LTV == 90`. |
| `Rules/Specifications/LowValueLtvBand1CreditScoreSpecificationTests.cs` | Applicability boundaries (`LoanAmount`, `LTV == 60`); satisfied boundary at `CreditScore == 750`. |
| `Rules/Specifications/LowValueLtvBand2CreditScoreSpecificationTests.cs` | Applicability boundaries at `LTV == 60` and `LTV == 80`; satisfied boundary at `CreditScore == 800`. |
| `Rules/Specifications/LowValueLtvBand3CreditScoreSpecificationTests.cs` | Applicability boundaries at `LTV == 80` and `LTV == 90`; satisfied boundary at `CreditScore == 900`. |
| `Rules/RulesEngineTests.cs` | Given hand-built fake `ISpecification<LoanApplication>` stubs (not the real 8), verifies: all-satisfied → approved; first inapplicable rule is skipped; first failing applicable rule's `Description` is returned as the decline reason and evaluation stops there (order matters). |
| `Statistics/LoanApplicationStatisticsTests.cs` | After recording a mix of approved/declined applications: applicant counts by outcome, total value written sums only approved amounts, mean LTV averages across all recorded applications regardless of outcome. |

## Files to create — `src/Finch.Console.IntegrationTests`

| File | Covers |
|---|---|
| `Fakes/FakeConsoleWriter.cs` | `IConsoleWriter` implementation that appends every line to an in-memory list instead of the real console — the substitution point, analogous to `FakeLogger`. |
| `LendingPlatformIntegrationTests.cs` | Builds the real DI container via `AddLendingPlatform()` (real 8 specifications, real `RulesEngine`, real `LoanApplicationStatistics`), swaps in `FakeConsoleWriter`. Drives several `LoanApplicationRequest`s end-to-end (skipping the interactive `ConsoleLoanApplicationReader`, since it wraps `Console.ReadLine`) through: map → evaluate → record → present. Asserts on: decisions for representative cases per rule category (general decline, high-value approve/decline, each low-value band approve/decline), and the final aggregate stats (counts, total value written, mean LTV) matching hand-calculated expected values for the batch submitted. |

Note: the interactive input loop in `ConsoleLoanApplicationReader`/`Program.cs` is not exercised by
automated tests — it's a thin wrapper around `Console.ReadLine` with no meaningful branching beyond
what `LoanApplicationFieldValidatorTests` already covers. This is called out as a known gap, not
silently skipped.

## `README.md` update (not yet written)

Once implementation is done, `README.md` gets: prerequisites (.NET 10 SDK), `dotnet run --project
src/Finch.Console`, `dotnet test src/Finch.sln`, a short usage walkthrough, and a pointer to
`CLAUDE.md` for the AI log required by the submission.

## Explicitly out of scope

Everything listed under **Future considerations** in `CLAUDE.md` (rule grouping, async rules,
rules persistence/versioning/audit, logging/traceability) — noted there, not built here.

---

**Nothing will be implemented until this plan is confirmed.** Flag anything you want changed —
file names/grouping, the exit-prompt UX assumption, the "no interface for
`LoanApplicationFieldValidator`" call, or the docs path substitution — and I'll revise before
starting.
