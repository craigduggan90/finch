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

- **Domain record**: a single `LoanApplication` (`LoanAmount`, `AssetValue`, `CreditScore`, plus a
  computed `LoanToValue`). There's no separate "raw request" type - see **AI collaboration log**
  for why an earlier two-type split was removed.
- **Field-level validation, not a batch validator**: each input field is validated (and
  re-prompted on failure) at the point of console input, one field at a time, rather than
  constructing a whole request object and validating it afterwards with a dictionary of errors.
  See **AI collaboration log** — this replaced an earlier design.
- **Specification pattern**: `ISpecification<LoanApplication>` with `IsApplicableTo`,
  `IsSatisfiedBy`, `Description`, and `Order`. Each of the 8 rules above is its own class — no
  combined/compound rules. Implementations aren't registered by hand: `AddLendingPlatform` reflects
  over the assembly (`RegisterImplementationsOf<TInterface>`) and registers every public,
  non-abstract type assignable to `ISpecification<LoanApplication>` automatically, so adding a new
  rule class is enough - no DI registration line to remember. All of them are injected into a
  `RulesEngine` as `IEnumerable<ISpecification<LoanApplication>>`; the engine sorts by `Order`
  (matching the table's numbering, 1–8) before evaluating, so evaluation order — and therefore
  which reason is returned on decline — is a stable, explicit contract rather than an accident of
  DI registration order.
- **Single console writer, single console reader**: exactly one class is allowed to call
  `System.Console.Write*` (`SystemConsoleWriter`, behind `IConsoleWriter`), and exactly one is
  allowed to call `System.Console.ReadLine` (`SystemConsoleReader`, behind `IConsoleReader`). Both
  are injected everywhere they're needed. This is what tests swap out (analogous to `FakeLogger` for
  `ILogger`) to drive `ConsoleLoanApplicationReader` with a scripted sequence of input - including
  EOF partway through - and assert on printed output, without touching the real console. See the AI
  log for when the reader side of this was added; it wasn't there from the start.
  - `ILogger` was deliberately **not** used for this output — routing decision/stats output through
    a logging abstraction would muddy the console output with log-level formatting, and there's no
    other sink to justify it right now.
- **DI container**: `Microsoft.Extensions.DependencyInjection`. `Program.cs` is the composition
  root. Integration tests build the same container (the real rule collection, resolved for real)
  and substitute only the `IConsoleWriter`.
- **Synchronous only**: nothing in the domain/business logic needs `async`/`await` — no I/O beyond
  console and no persistence. See Future considerations. The one `await` in the codebase,
  `await using var provider = services.BuildServiceProvider();` in `Program.cs`, is DI container
  lifecycle plumbing (proper async disposal of the `ServiceProvider`, not business logic) and isn't
  a violation of this - flagging it here since automated PR reviewers keep raising it as if it were.
  `ISpecification`, `RulesEngine`, validation, and statistics remain entirely synchronous.

## Future considerations (out of scope for the 1h timebox)

- **Rule grouping/relationships**: the 8 rules are currently flat and independent. The spec's
  cascading structure hints at families (general limits vs. high-value vs. low-value-by-band) that
  could become explicit groupings later — e.g. if new loan categories are added, or if rules need
  shared metadata (a category tag, a severity, an owner).
- **Only the first failure is surfaced.** Because decline reasons are "first failing rule in
  evaluation order," fixing one problem in a UI/API consumer could just reveal the next one
  ("whack-a-mole"). The likely fix: run every rule regardless of earlier failures and return an
  array of failure reasons instead of a single string - noted here as a direction, not built, since
  it changes `LoanDecision`'s shape and every caller of it.
- **Nothing enforces `Order` uniqueness.** Each `ISpecification<T>` declares an explicit `Order`
  (its doc comment says it should be unique, but says so only in a comment), and `RulesEngine`
  sorts by it before evaluating, which is a real improvement over trusting DI registration order
  (see AI log below) - but two specifications could still declare the same `Order` today, and
  nothing would catch it at compile time, at registration, or at `RulesEngine` construction. LINQ's
  `OrderBy` is a stable sort, so ties would silently fall back to enumeration order - quietly
  reintroducing the exact problem `Order` was added to close. We *could* have `RulesEngine`'s
  constructor group the injected specifications by `Order` and throw if any group has more than
  one member - deliberately not done yet, not because it's hard, just not needed until it bites.
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
  field, in a loop. EOF (`Ctrl+D`/`Ctrl+Z`) at any prompt ends the session cleanly rather than
  requiring the process to be killed - see the AI log's EOF-handling entries. What's still missing:
  there's no way to abandon just the *current* in-progress application and start a fresh one
  without ending the whole session - EOF quits outright, it doesn't offer a "cancel this one and
  retry" option. This is an accepted limitation of a plain console app without a TUI library.
- **`FakeConsoleWriter` is duplicated, not shared.** It exists identically in both
  `Finch.Console.UnitTests/Fakes` and `Finch.Console.IntegrationTests/Fakes` (see AI log). Craig
  doesn't want a dependency between the two test projects, and a third shared-test-utilities
  project isn't worth it for one ~10-line class today. Noted here deliberately rather than acted
  on - if more shared test doubles show up later, that's the point to revisit extracting one.

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

### Bug fix: infinite loop on piped/redirected EOF

Craig caught this by inspection, not by running anything: `ConsoleLoanApplicationReader`'s field-
reading loops treated `Console.ReadLine()` returning `null` (EOF - stdin closed, as happens when
piped input runs out) identically to unparseable text - print "Enter a valid number." and read
again. But a closed stdin keeps returning `null` forever, so the loop never terminates short of
killing the process. `ShouldReadAnotherApplication` already handled the same `null` case correctly
via `?.Trim()`; the field readers didn't.

This directly undermined the piped-stdin verification approach used throughout this log and
described in the README, and would hang any script or CI step that drives the app non-interactively
with a finite input file. It's also reachable interactively, not just via piping: a user can send
the same EOF at any prompt with `Ctrl+D` (macOS/Linux) or `Ctrl+Z`+`Enter` (Windows) - documented in
the README as a normal way to end a session, not just a CI/scripting concern.

Fixed by making EOF a distinct, handled case: `ReadDecimalField`/`ReadIntField` now check `raw is
null` before attempting to parse, print a one-time "No more input received" message, and return
`null` (hence `decimal?`/`int?`) instead of looping; `ReadApplication()` (now
`LoanApplicationRequest?`) returns `null` as soon as any field hits EOF, rather than trying to
assemble a request from incomplete data; `Program.cs` breaks its loop on a `null` request the same
way it already relied on `ShouldReadAnotherApplication` returning `false`.

Verified directly rather than assumed fixed: re-ran the exact repro (`echo "100000" | dotnet run
...`), confirmed exit code 0 instead of a hang, then also checked EOF on the very first prompt and
EOF immediately after a completed application (the pre-existing correct path) to make sure nothing
regressed.

### Simplification: consolidated ReadDecimalField/ReadIntField

Craig pointed out `ReadDecimalField` and `ReadIntField` were near-identical - same prompt/loop/EOF/
validate structure, differing only in which `TryParse` overload and parse-error message they used.
Consolidated into a single `private T? ReadField<T>(string prompt, Func<string, T?> tryParse,
string parseErrorMessage, Func<T, FieldValidationResult> validate) where T : struct`.

This is the same generic-inference territory that broke the first time (logged above, under
"Implementation pass") - but a different, working shape. The earlier failure was referencing a bare
generic *type*'s static members without type arguments (`FieldParseResult.Ok(value)`), which
doesn't compile. This time it's a generic *method*, called with `Func<string, T?>` lambdas - but
even so, the compiler couldn't infer `T` from `raw => decimal.TryParse(...) ? value : null` on its
own (`CS0411`), so call sites specify it explicitly: `ReadField<decimal>(...)` /
`ReadField<int>(...)`. Confirmed by building (it failed first, exactly as described, before the
explicit type arguments were added) rather than assuming the generic method would just infer.

Re-verified after the change: full test suite (84/84, none of this is exercised by tests - see the
known gap noted earlier), plus manually re-ran all three interactive paths this file touches -
normal completion, invalid-input retry (non-numeric then negative), and the piped-EOF fix from
directly above - to make sure consolidating the two methods didn't quietly change behaviour in any
of them.

### Simplification: shared `HighValueThreshold` base class

Craig pointed out the £1m threshold (`private const decimal HighValueThreshold = 1_000_000m`) was
copy-pasted verbatim into 6 of the 8 specification classes (both high-value rules, and all four
low-value rules that key off "under £1m"). Added `HighValueThresholdSpecification`, an abstract
class holding just that one `protected const`, and had the six affected classes inherit it
alongside still implementing `ISpecification<LoanApplication>` directly (a class can have one base
class and implement interfaces at the same time - no conflict with the specification pattern).

Deliberately left alone: the LTV band boundaries (`60m`, `80m`, `90m`) that appear in adjacent
band specifications - e.g. `60m` is band 1's ceiling and band 2's floor. Those are the same *value*
in different classes, but not the same *constant* being copy-pasted (different name, different
role, no shared source of truth to extract without coupling two classes' band edges together in a
way that wasn't asked for). Flagging this distinction rather than silently going further than the
request.

### Simplification: merged `LoanApplicationRequest` into `LoanApplication`

Craig asked whether the `LoanApplicationRequest` → `LoanApplication` split was still needed. It
wasn't: the split's only reason to exist was so `LoanApplication` could assume a non-zero
`AssetValue` without a div-by-zero guard, which mattered when a *batch* validator was going to
construct a request and validate it afterwards. That design was replaced early on (see "AI
collaboration log" further up) with field-level validation that happens *before* any object is
assembled - so by the time either type could be constructed, every field is already guaranteed
valid. Once that pivot happened, the two-type split stopped doing anything; it just wasn't cleaned
up until asked about directly.

Removed `LoanApplicationRequest.cs` and `LoanApplication.FromRequest` entirely.
`ConsoleLoanApplicationReader.ReadApplication()` now builds and returns `LoanApplication?` directly,
and `Program.cs`/tests were updated accordingly. Also dropped `LoanApplication`'s XML doc comment
(the "mapped from a validated request, assumes AssetValue > 0" text no longer described anything
that exists) - the record's field names already say what it is, and the div-by-zero-safety
invariant is still true, just no longer worth a comment now there's no second type to contrast it
against.

### Clarification: `await using` on the container isn't a sync violation

Automated PR reviewers kept flagging `await using var provider = services.BuildServiceProvider();`
in `Program.cs` against the "synchronous only" design note. Clarified in the Architecture section:
that line is DI container disposal plumbing, not business logic going async - `ISpecification`,
`RulesEngine`, validation, and statistics are still entirely synchronous. No code changed, only the
doc, to give automated reviewers (and future readers) the context to stop flagging it.

### Bug fix: silent exit on EOF at the "another application?" prompt

Craig noticed the EOF fix didn't cover every prompt: `ShouldReadAnotherApplication` treated a
`null` `Console.ReadLine()` result as just another "no", trimming it via `?.Trim()` and comparing
against `y`/`yes` - which correctly stopped the loop, but printed nothing, leaving a user who hit
EOF here (as opposed to mid-field) with no explanation for why the session ended. Fixed by giving
it the same explicit `null` check and "No more input received" message as the field readers, rather
than relying on it happening to fail the y/n comparison silently.

While doing this, extracted the repeated message-printing into a `WriteEndOfInputMessage()` helper
shared by `ReadField<T>` and `ShouldReadAnotherApplication`, instead of duplicating the literal
string in both places.

### Simplification: extracted `TryParseDecimal`

Craig caught this mid-edit: `ReadApplication()`'s loan amount and asset value calls to `ReadField`
passed identical inline `raw => decimal.TryParse(...) ? value : null` lambdas - copy-pasted, not
just similar. Extracted a `private static decimal? TryParseDecimal(string raw)` local method and
pointed both call sites at it. The credit score call keeps its own inline `int.TryParse` lambda,
since it's the only place that needs it - nothing to extract there.

### Noted, not fixed: `Order` has no uniqueness guarantee

Craig flagged that nothing stops two specifications from declaring the same `Order` - ties would
silently fall back to a stable-sort tiebreak on enumeration order, which is the exact DI-ordering
fragility `Order` exists to remove. Explicitly deferred rather than fixed: Craig's own likely fix
is broader than a uniqueness check - running every rule regardless of earlier failures and
returning an array of reasons instead of a single first-match string - so a narrow "reject
duplicate Order" guard now would be solving a smaller problem than the one actually worth solving
later. Documented as a future consideration in the Architecture section rather than built.

### Change request: minimum currency amount is £0.01, not "greater than zero"

Craig pointed out that `> 0` was the wrong lower bound for currency fields - it let sub-penny
fractional values like `0.005` through, which isn't a real GBP amount. Changed
`ValidateLoanAmount`/`ValidateAssetValue` from `<= 0` to `< 0.01m`, which rejects zero, negative
values, and fractional pennies alike, with the error message updated to "must be at least £0.01."
Added a test case for a sub-penny value (`0.005`) to both fields' invalid-input theories, since the
old test suite only exercised whole-zero and negative boundaries - it would have missed this gap
entirely.

### Change request: revert the specification base class to a plain constants holder

Craig reconsidered the `HighValueThresholdSpecification` base class from earlier in this log: it
existed purely to share one constant, but inheritance implies shared *behaviour* to a reader, not
just a shared value - misleading for anyone skimming the six classes that "inherit" it. Reverted:
deleted the base class, added `RuleThresholds` (a plain `public static class` holding `const decimal
HighValueThreshold`), and had the six specifications reference `RuleThresholds.HighValueThreshold`
directly instead of inheriting it. Each of those six classes is back to only implementing
`ISpecification<LoanApplication>`, nothing else in its base list.

### Change requests: asset value ceiling removed, loan amount ceiling now derived

Two related corrections in the same breath. First, Craig pointed out the asset-value upper bound
was pointless now that asset value has a sane minimum (0.01): `LoanToValue = (LoanAmount /
AssetValue) * 100` only gets *smaller* as asset value grows, so a large asset value was never an
overflow risk - the ceiling on it was solving a problem that didn't exist. Removed it; asset value
now only has a floor.

Second, Craig asked for the loan amount ceiling itself to stop being a magic number
(`999_999_999_999_999.99m`, chosen somewhat arbitrarily as "1 quadrillion minus a penny") and
instead be *derived*: `decimal.MaxValue / 10_000m`. The reasoning ties the two changes together -
`LoanToValue` is `LoanAmount * (100 / AssetValue)`, and since asset value's floor is `0.01`, the
worst-case multiplier is `100 / 0.01 = 10,000`. Capping loan amount at `decimal.MaxValue / 10,000`
guarantees that multiplication can't overflow `decimal`, even at that worst case - a real derivation
instead of a round-sounding literal. Also switched the "loan amount too large" error message from
spelling out that ugly derived number (`£79,228,162,514,264,337,593,543,950.03...`-ish) to a plain
"Loan amount is too large." - the exact figure was never meant to be user-facing.

Tests updated to match: the loan-amount boundary tests now compute `decimal.MaxValue / 10_000m`
themselves rather than duplicating a literal, and the asset-value tests replace the old ceiling
boundary pair with a single test proving a very large asset value is still valid.

### Simplification: made `ValidateCreditScore` consistent with `ValidateLoanAmount`

Craig pointed out `ValidateLoanAmount` (a `switch` expression, two distinct branches) and
`ValidateCreditScore` (a ternary over `creditScore is >= 1 and <= 999`, one combined message for
either direction) had drifted into two different styles for the same shape of problem - and the
switch form has a real advantage, not just consistency: it can tell "too low" apart from "too high"
in the message. Converted `ValidateCreditScore` to the same `switch` shape, splitting "Credit score
must be between 1 and 999." into "must be at least 1." / "must not exceed 999.". `ValidateAssetValue`
was left as a ternary - it only has one condition (a floor, no ceiling, per the earlier change), so
there's nothing for a switch to add there. Existing tests didn't assert exact message text, so they
kept passing unchanged; verified the new messages manually via the running app regardless, rather
than trusting that a passing test suite meant the visible behaviour was right.

### Change request: cover the EOF/Ctrl+D logic that coverage flagged as untested

Craig ran coverage and noticed the EOF-handling work from earlier in this log - a real bug fix, not
throwaway plumbing - had zero test coverage, and asked (rhetorically - "it's like a PR comment,
I'm asking, but really I'm telling") whether it should be. Agreed it should: this was exactly the
code that had a real infinite-loop bug in it, so "thin wrapper, not worth testing" (the original
scoping call in the implementation plan) no longer held once that history existed.

Closed the gap by mirroring the existing `IConsoleWriter` pattern on the input side: added
`IConsoleReader` (one method, `ReadLine()`) and `SystemConsoleReader` (the sole class now permitted
to call `System.Console.ReadLine`), and changed `ConsoleLoanApplicationReader` to depend on
`IConsoleReader` instead of calling `System.Console.ReadLine()` directly. Registered it in
`AddLendingPlatform` alongside the writer.

Added `Finch.Console.UnitTests/Fakes/FakeConsoleReader` - returns a queued sequence of lines, then
`null` forever, so a test can simulate EOF at any exact point just by how many lines it queues up.
Duplicated `FakeConsoleWriter` into the unit tests project too (previously only in
`IntegrationTests`) rather than adding a project reference between test projects for one tiny class.

New `ConsoleLoanApplicationReaderTests` (16 tests) covers what had no coverage before: the happy
path, re-prompting on parse and validation failure, and - the actual point of this exercise - EOF
during each of the three fields and at the "another application?" prompt, asserting both the
returned value (`null`/`false`) and that the right message was written. Verified the real DI-wired
app still behaves identically end-to-end afterward, not just that the new unit tests passed in
isolation.

### Correction: stale "no way to cancel" claim in Future considerations

Craig caught that the "Console UX for invalid input" bullet still claimed there was "no way to
cancel an in-progress application short of killing the process" - true when it was written, but
made false by the EOF/Ctrl+D handling added later in this same log. Doc-only fix: updated the
bullet to reflect that EOF now ends the session cleanly, and narrowed the actual remaining gap to
what's still true - there's no way to abandon just the *current* application and start a new one
without ending the whole session outright.

### Noted, not fixed: `FakeConsoleWriter` duplication across test projects

When `IConsoleReader` was added, `FakeConsoleWriter` got duplicated into
`Finch.Console.UnitTests/Fakes` rather than reused from `Finch.Console.IntegrationTests/Fakes`,
since unit tests don't (and per Craig, shouldn't) reference the integration test project. Craig
confirmed the trade-off explicitly rather than letting it sit as an unstated assumption: no
cross-test-project dependency, and a dedicated shared-test-utilities project isn't worth it yet for
one tiny class. Recorded as a future consideration instead of resolved now.

### Simplification: auto-register `ISpecification<T>` implementations by reflection

Craig found hand-registering all 8 rules in `AddLendingPlatform` a nuisance and supplied the
replacement directly: a `RegisterImplementationsOf<TInterface>` helper that scans
`typeof(TInterface).Assembly` for every public, non-abstract type assignable to `TInterface` and
registers each as a singleton. Adopted as given - `typeof(TInterface).IsAssignableFrom(type)` works
correctly against a *closed* generic interface like `ISpecification<LoanApplication>` (nothing here
is an open generic type needing special-case handling), and every specification class already lived
in the same assembly as the interface, so no assembly-selection logic was needed beyond
`typeof(TInterface).Assembly`.

Net effect: adding a new rule class is now enough on its own - there's no DI registration line to
remember or forget. Verified rather than assumed: ran the full suite (100/100, including the
integration tests that build the real container and exercise all 8 rules end-to-end) and manually
drove the running app through a high-value and a low-value-band case to confirm reflection
discovered every rule at actual runtime, not just under test.

### Bug fix: `LoanApplicationStatistics` could overflow after just two applications

Craig spotted this via code review rather than running anything: deriving the loan amount ceiling
from `decimal.MaxValue / 10,000` (see the change above) means a single application's `LoanToValue`
can now land at exactly `decimal.MaxValue`. `LoanApplicationStatistics` summed every recorded
`LoanToValue` into a running total (`_totalLoanToValue += ...`) and divided by count only when
`MeanLoanToValue` was read - so after one application at that extreme, recording *any* second
application (even an ordinary one) computed `decimal.MaxValue + <anything positive>`, an unhandled
`OverflowException` that would crash the whole interactive session. Before the ceiling was derived
this precisely, reaching that state took on the order of 10^9 applications; the tighter, correct
ceiling made it reachable in exactly two.

Tightening the ceiling further wouldn't fix this - it only moves the threshold back out, and is
reachable again given enough applications in a long-running session. The actual fix is to stop
summing an unbounded quantity: `MeanLoanToValue` is now maintained as an incremental running mean
(`mean += (newValue - mean) / count`), the same identity Welford's algorithm uses. This is an exact
identity, not an approximation - algebraically equal to `sum / count` - and because a mean can never
exceed the largest individual value it's computed from, and every `LoanToValue` is itself bounded to
`decimal.MaxValue` by the validator, the running mean is provably bounded in the same range at every
step. Verified this holds by walking the algebra through by hand (see the chat transcript) before
writing any code, then added a test reproducing the exact failure: one application with `LoanAmount
= decimal.MaxValue / 10_000m, AssetValue = 0.01m` (driving `LoanToValue` to exactly
`decimal.MaxValue`) followed by an ordinary second application, asserting the exact expected mean
`decimal.MaxValue / 2m + 25m` - computed that way rather than as `(decimal.MaxValue + 50m) / 2m` so
the *test's* expected-value calculation doesn't reproduce the same overflow it's checking for.
Existing tests needed no changes, since the identity guarantees identical results to the old
`sum / count` approach for every value that doesn't overflow it. Also reproduced the exact scenario
against the real running app (not just the unit test) to confirm the session no longer crashes.

This log will be extended as implementation proceeds — further iterations, corrections, or
questioned AI output belong here, per the test's requirement to document AI usage.
