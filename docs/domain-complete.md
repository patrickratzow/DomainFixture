# Domain Complete direction

DomainFixture should generate tests from a domain contract, not from one validation library.
FluentValidation is an adapter that contributes constraints; nullability, naming conventions,
constructors, attributes, and future user-defined adapters can contribute the same model.

The generation pipeline is:

1. Discover subject structure, recipes, and external rule sources.
2. Adapt those sources into framework-neutral `DomainConstraintSpec` values.
3. Discover constructors and conventional factories as framework-neutral operations.
4. Discover inferred and adapter-supplied operation outcomes and domain scenarios.
5. Normalize every discovered fact into one provenance-aware `DomainTypeSpec` per subject.
6. Resolve valid instances through recipe baselines or construction/value-provider synthesis.
7. Resolve each constraint through a boundary-case provider.
8. Resolve each required mutation through the mutation strategy pipeline.
9. Project invalid boundaries through operations with explicit rejection outcomes.
10. Resolve each domain scenario through a scenario provider.
11. Emit factories, tests, a visible spec snapshot, a coverage report, and diagnostics from that model.

The current executable slice covers presence, text length, Int32 ranges, immutable reconstruction,
synchronous validation execution and rejection, inferred value-object equality laws, construction
round trips, and synchronous entity transitions. Records use a state-preserving `with` copy;
conventional value objects use a real constructor or static factory. Construction discovery maps
accessible constructor parameters and `From`/`Create`/`Of` parameters to readable properties by
name and type, falling back only when a type match is unique.
A recognized rule without an adapter produces `DFG015`; an interpretable constraint without a
boundary provider produces `DFG017`. Unsupported or unresolved scenarios produce `DFG021` through
`DFG024`. Ambiguous operation parameter mapping produces `DFG025`. Missing coverage must not be
silent.

Further domain-complete scenario families should be added beside validation rather than embedded
inside its adapters:

- collection/count constraints and richer collection mutation semantics;
- result-wrapper outcomes and async operations;
- explicit user-defined domain scenarios for rules that cannot be inferred safely.

Inference should provide useful defaults, while explicit scenarios remain the escape hatch for
business meaning that source analysis cannot recover.

## External integrations

`DomainFixture.Contracts` is dependency-free and contains stable kind identifiers, versioned
constraint and scenario envelopes, and deterministic provider-pipeline primitives. An external
analyzer adapts its own domain framework into `DomainContractManifestAttribute` metadata and can
contribute domain laws through `DomainScenarioManifestAttribute`. It can also describe
non-conventional callable APIs and their outcomes with `DomainOperationManifestAttribute` and
`DomainOperationOutcomeManifestAttribute`. The test-side generator reads all four metadata forms
without loading, instantiating, or executing the adapter assembly.

Scenarios are peers of validation constraints rather than special cases inside FluentValidation.
A fixture may therefore generate scenario tests without having a validator at all. The first
built-in provider handles `domainfixture.law.value-object-equality` and emits reflexivity, symmetry,
and equal-value hash-code laws.

## Construction operations

`DomainOperationContract` is the stable intermediate representation between callable discovery and
scenario generation. It records the operation kind, declaring and subject types, member name, and
ordered parameter-to-property bindings. The construction scenario provider currently emits a
baseline-success test and a property round-trip test for every mapped operation. It works without a
validator, generation profile, DI container, or runtime reflection.

Invalid-boundary rejection is deliberately not inferred from factory names. A constructor may be a
plain state carrier, a factory may throw, and another factory may return a result object. Synchronous
exception rejection is therefore declared centrally:

```csharp
options.Operations()
    .RejectWith<DomainException>()
    .RejectWith<Username, ValidationException>();
```

An exact operation outcome manifest has highest precedence, followed by a subject-specific profile
declaration and then the assembly-wide default. Invalid boundary cases are sent through the real
constructor or factory: only the targeted argument is replaced, and all other arguments come from a
fresh recipe baseline. With no explicit outcome, no rejection test or warning is generated.

Synchronous generic result wrappers are configured by shape rather than package:

```csharp
options.Operations()
    .UseResult<ResultDisplayName, DomainResult<ResultDisplayName>>(
        result => result.IsSuccess,
        result => result.Value);
```

The selectors are compile-time checked and normalized to readable member paths. A conventional
`From`/`Create`/`Of` factory may return any configured wrapper type. `.Synthesize()` invokes the
factory, verifies success, and unwraps the subject. Construction tests assert success and round-trip
the unwrapped value; invalid constraint boundaries invoke the same factory and assert failure.
DomainFixture never references or instantiates a particular Result library.

Metadata adapters can contribute the same `domainfixture.operation-outcome.result` outcome. The
manifest is accepted only when the success path is a readable `bool` and the value path has the
subject type. Exact metadata takes precedence over assembly-profile configuration.

## Typed recipe factories

Every recipe baseline also produces a test-framework-neutral, strongly typed fixture factory for
handwritten tests:

```csharp
var username = UsernameFixtureFactory.Validation.Create();
var approved = RegistrationFixtureFactory.Approved.Create();
var internalHandle = QualifiedHandleFixtureFactory.Validation.Create(
    value => value with { Realm = "internal" });
```

`Create()` invokes the recipe baseline on every call, so instances are fresh and isolated. The
baseline is the recipe's valid-instance guarantee. The transform overload starts from that valid
instance, but the caller owns the validity of the transformed result; it is intentionally not
revalidated or normalized. Factories require no runtime registry, reflection, DI container, or
string-based scenario lookup.

Generated test and factory bodies use a per-file alias plan instead of repeating fully qualified
names. A unique domain type becomes a short alias such as `Username`; same-name types receive
deterministic namespace-derived aliases such as `SalesDomainOrder` and `ShippingDomainOrder`.
Generated-scope identifiers are treated as conflicts, common framework names use safe namespace
imports, and the generator retains `global::` only where shortening would be ambiguous. Canonical
domain-spec identities remain fully qualified; aliasing is an emission-only readability feature.

A recipe can request deterministic construction instead of maintaining a hand-written baseline:

```csharp
fixture.Recipe("Valid")
    .Synthesize();
```

Assemblies that want synthesis to be the default can enable it once in their generation profile:

```csharp
options.Conventions()
    .AutoSynthesizeRecipes();
```

With that convention, a source-less `fixture.Recipe("Valid");` is equivalent to explicitly calling
`.Synthesize()`. `Baseline(...)` and `FromTransition(...)` remain explicit overrides, and conflicting
explicit sources are still rejected. Without the convention, source-less recipes continue to emit
`DFG001`.

The valid-instance pipeline prefers an explicit baseline, then tries discovered constructors and
`From`/`Create`/`Of` factories in stable order. Arguments come from intersected string and `Int32`
constraints, deterministic bounded primitives, fresh `Guid` providers, assembly-wide unique strings,
nested recipes, inferred value-object
factories, and common arrays/lists.
An uncovered or cyclic parameter emits `DFG042`; the generator never silently substitutes an unsafe
guess.

Referenced value objects are inspected recursively at compile time. Inference prefers accessible
`From`/`Create`/`Of` factories, then a single unambiguous static method returning the value type, then
an accessible constructor. Its arguments use the same pipeline, so shapes such as
`OrderId.From(Guid)`, `Sku.From(string)`, and `Money.Usd(decimal)` require no profile entries.
Unconstrained integral and decimal baselines use positive `1`; explicit constraints still determine
the actual in-range value. Nested fixture recipes take precedence over conventional value inference.

Nested recipes compose through their generated typed factories. Element resolution is recursive, so
arrays, lists, sets, `Collection<T>`, and dictionary interfaces can contain primitives, configured
values, or other fixture-backed domain types. Collections contain one deterministic element by
default. Recursive type graphs are traced during planning and report their construction cycle rather
than emitting mutually recursive factories.

The assembly profile is the central escape hatch when a convention cannot know the intended business
choice, such as an enum whose first member is not a valid baseline or a value object with several
equally meaningful factories:

```csharp
options.Values()
    .For<BillingCycle>(() => BillingCycle.Monthly)
    .For<Currency>(() => Currency.Eur);
```

Configured values are compile-time expressions, not delegates executed by the generator. Literals,
static fields/properties, constructors, and static factory calls are supported. Duplicate values
produce `DFG045`; runtime-dependent expressions produce `DFG044`. The configured-value provider runs
before every built-in or inferred convention, so an explicit value never conflicts with an inferred
fallback. It is shared by synthesized factories and `FixtureValue.Auto<T>()` command arguments.
Configured values also appear in the generated normalized-spec report.

## Normalized spec and coverage

`DomainTypeSpec` groups recipes, readable properties, constraints, construction operations,
outcomes, and scenarios while retaining whether each fact was inferred, discovered, manifested, or
configured by the assembly profile. Stable operation IDs are merged once, and incompatible facts
produce explicit conflicts.

`DomainFixture.DomainSpecSnapshot.g.cs` and `DomainFixture.DomainCoverageReport.g.cs` make those facts,
their provenance, factory/test classification, and uncovered reasons visible under generated files.
They are compile-time reports only: there is no reflection, registry, DI container, or runtime cost.

## Entity transitions

Recipes can declare synchronous state transitions and rejected commands. With the entity-identity
convention enabled, the generator separately verifies the expected state and preservation of `Id`
(or `<SubjectName>Id`). The executable `Registration` example proves that a Pending registration can
be approved, retains its identity, and that approving an already Approved registration throws the
declared exception. Pending and Approved recipes also expose independent typed factories for use by
handwritten tests.

Commands may use explicit arguments or `FixtureValue.Auto<T>()`, which delegates to the same value
pipeline used by synthesized factories. Synchronous immutable commands can return the next aggregate,
result-returning commands can declare a predicate, and named state expectations support nested paths:

```csharp
fixture.Recipe("Valid")
    .Synthesize()
    .State("Initially pending", x => x.Details.Status, SubscriptionStatus.Pending)
    .Transition(
        "Activate",
        x => x.Activate(FixtureValue.Auto<int>()),
        x => x.Status,
        SubscriptionStatus.Active)
    .Transition(
        "Can reserve",
        x => x.CanReserve(FixtureValue.Auto<int>()),
        result => result);
```

Valid state recipes can be derived from an earlier recipe's successful transition instead of
repeating its construction values and mutation sequence:

```csharp
fixture.Recipe("Dispatched")
    .FromTransition("Pending", "Dispatch");

fixture.Recipe("Delivered")
    .FromTransition("Dispatched", "Deliver");
```

The generator creates the source recipe, resolves automatic transition arguments through the same
value pipeline, applies the synchronous command, and returns a fresh aggregate in the requested
state. Immutable subject-returning commands are reassigned automatically. Rejection and arbitrary
result transitions cannot serve as valid-state edges. Missing recipes/transitions, uncovered command
arguments, and cyclic state graphs produce `DFG042` instead of generating a partial factory.

The `.Domain` acceptance suite snapshots the generated `Subscription` factory and its construction,
result, immutable-state, identity, and nested-state tests.

Async commands/results, error-payload assertions, method-based result selectors, property-specific
generated `With` methods, and runtime DI activation remain intentional later extensions. Synchronous
execution remains the primary path.

Legacy validation manifests remain readable, but new adapters should emit the generic manifests.
Unknown schema versions, unhandled contract kinds, ambiguous providers, and invalid provider input
all produce explicit generator diagnostics.
