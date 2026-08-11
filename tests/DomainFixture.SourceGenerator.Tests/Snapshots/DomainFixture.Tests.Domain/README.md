# Visible generated `.Domain` tests

The `.verified.cs` files are the exact NUnit sources generated from:

- `DomainFixture.Tests.Domain.Entities.RegistrationRequestValidator` in the real `.Domain` assembly;
- `DomainFixtureProfile.cs` in the test project;
- `RegistrationRequestFixture.cs` in the test project;
- `DomainFixture.Tests.Domain.ValueObjects.UsernameValidator` in the real `.Domain` assembly;
- `UsernameFixture.cs` and its centrally configured reconstruction strategy;
- `QualifiedNameFixture.cs`, which proves that reconstruction preserves the untouched property of
  a two-property immutable value object;
- `QualifiedHandleFixture.cs`, which proves that a sealed record uses `with` reconstruction and
  preserves its other positional properties while exercising text minimum/maximum and Int32 range
  boundary providers;
- `RegistrationFixture.cs`, which supplies deterministic Pending and Approved aggregate scenarios.
- `SubscriptionFixture.cs`, which has no hand-written baseline and exercises synthesized valid
  construction, automatic command arguments, immutable/result transitions, and a nested state path.
- `ResultDisplayNameFixture.cs`, whose conventional `Create` factory returns
  `DomainResult<ResultDisplayName>` instead of the subject directly.

The `Username` and `QualifiedHandle` snapshots also expose the inferred value-object equality
scenario. Each gets reflexivity, symmetry, and equal-value hash-code tests. `Username` creates a
true peer through its static factory; `QualifiedHandle` uses an empty record `with` expression.
These semantic copies are deliberately independent from the invalid-boundary mutation machinery.

Construction tests are visible for three different operation shapes:

- `Username` uses an inherited generic `From` factory whose `item` argument is mapped to `Value` by
  its unique type;
- `QualifiedName` uses a two-property `From(name, realm)` factory;
- `QualifiedHandle` uses its three-parameter record constructor.

Each operation gets a baseline-success test and an argument round-trip test. These suites require no
fixture-level construction configuration and can be generated even when no validator exists.

`Username` also shows explicit synchronous operation rejection. The assembly profile declares the
subject-specific `ValidationException` outcome once, and the generated tests invoke the real `From`
factory with empty, null, and above-maximum inputs. Rejection is never guessed from a factory name.
Outcome precedence is exact manifested operation, subject-specific profile declaration, then
assembly-wide profile default. Other construction operations keep round-trip coverage without
invented rejection assertions.

The `*.Factory.verified.cs` files show the test-framework-neutral API available to handwritten tests:

```csharp
var username = UsernameFixtureFactory.Validation.Create();
var pending = RegistrationFixtureFactory.Pending.Create();
var approved = RegistrationFixtureFactory.Approved.Create();
```

Each `Create()` evaluates its recipe baseline again. The transform overload receives a fresh valid
baseline, but any invariant broken by the transform is the caller's responsibility; generated
factories do not silently revalidate transformed values.

The snapshots intentionally show the generated alias policy. Test bodies use short domain and
framework names; alias directives keep the exact compile-time binding visible once at the top of
the file. Short-name collisions are resolved with stable namespace-derived names, while genuinely
ambiguous references keep their `global::` qualification.

The Registration snapshots extend coverage beyond validators. The Pending recipe proves `Approve`
reaches `Approved` and preserves `Id`; the Approved recipe proves a second approval synchronously
throws `InvalidOperationException`. Their generated factories are exercised by handwritten tests to
show that state changes never leak into later creations.

The Subscription snapshots make the normalized domain-spec direction concrete. `.Synthesize()`
selects `Subscription.Create` and supplies a deterministic string and integer plus a fresh `Guid`. The same
expression powers the typed factory and all generated tests. `FixtureValue.Auto<int>()` supplies
command arguments, `Activate` is treated as an immutable transition that preserves identity,
`CanReserve` is checked through a synchronous result predicate, and `Details.Status` proves named
nested-state assertions.

The TenantSubscription snapshots show recursive object-graph synthesis. Its factory composes the
existing `UsernameFixtureFactory`, recursively builds a username list and role dictionary, and uses
the assembly profile's deterministic `BillingCycle.Monthly` value. The generated construction tests
consume that same typed factory, while a handwritten test proves the complete graph is immediately
usable without DI or a runtime registry.

The ResultDisplayName snapshots demonstrate wrapper-neutral outcomes. The central profile selects
`IsSuccess` and `Value` with typed expressions. Generated factories unwrap successful results;
construction tests verify success and round-trip the value; empty, null, and above-maximum boundaries
assert failure on the real result-returning factory.

The test-project generation profile enables FluentValidation once for the assembly. Fixtures only
provide their baseline; the generator matches the validator by subject type and emits the adapter
shown in each snapshot. `ValidateWith(...)` and `RulesFrom<TValidator>()` remain optional overrides.

Run the snapshot check with:

```powershell
dotnet test tests/DomainFixture.SourceGenerator.Tests/DomainFixture.SourceGenerator.Tests.csproj `
  --filter FullyQualifiedName~DomainGeneratedSuiteSnapshotTests
```

When generation changes, the test reports the first mismatched line and prints the complete newly
generated source. Review that output and update the `.verified.cs` file only when the change is
intentional.

To refresh all reviewed snapshots after inspecting a deliberate generator change, run the same
test with `--environment "UPDATE_DOMAIN_FIXTURE_SNAPSHOTS=1"`, then rerun it normally.

Framework integrations adapt into domain constraints before these tests are generated. External
analyzers can additionally contribute non-conventional operations and exact outcomes through
metadata-only operation/outcome manifests; the consumer generator never loads or runs the adapter.
Recognized rules without an adapter or boundary provider produce diagnostics instead of disappearing.

Async commands/results, result error-payload assertions, richer collection mutation,
property-specific generated `With` APIs, and runtime DI activation remain intentionally deferred.
