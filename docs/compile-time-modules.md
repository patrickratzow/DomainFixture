# Compile-time modules

DomainFixture modules extend discovery without runtime plugin loading, reflection, dependency
injection, or a reference from DomainFixture to the integrated framework. A module is an analyzer
package that translates framework-specific symbols into versioned, framework-neutral assembly
metadata.

```text
producer compilation                     test compilation
--------------------                     ----------------
FluentValidation / MediatR symbols
        |
module analyzer
        |
module + contribution manifests  --->    DomainFixture source generator
                                         |
                                         normalized DomainTypeSpec
                                         |
                                         factories + generated tests
```

Roslyn generators cannot inspect source emitted by another generator during the same compilation.
For that reason, contribution manifests are compiled into the producer assembly and consumed from
that assembly reference by the downstream test project. A module may also run in the test project
to emit adapter classes whose deterministic names were recorded in producer metadata; all generated
trees are compiled together even though generators cannot semantically inspect one another's trees.

## Stable contracts

`DomainFixture.Contracts` remains `netstandard2.0` and dependency-free. It defines:

| Contract | Purpose |
| --- | --- |
| `DomainFixtureModuleManifestAttribute` | Module ID, version, source marker, and capabilities |
| `DomainFixtureRecipeManifestAttribute` | Adds an explicit synthesized recipe root |
| `DomainFixtureValidationManifestAttribute` | Binds a subject to a module-owned `IFixtureValidator<T>` adapter |
| `DomainContractManifestAttribute` | Constraints and validation boundaries |
| `DomainScenarioManifestAttribute` | Domain laws and scenarios |
| `DomainOperationManifestAttribute` | Constructors, factories, and other callable operations |
| `DomainOperationOutcomeManifestAttribute` | Exception or result outcomes for operations |

Known capability IDs are exposed through `DomainFixtureModuleCapabilities`: `Constraints`,
`Scenarios`, `Operations`, `OperationOutcomes`, `Recipes`, and `Validators`. A registered module may
only contribute the families it declares. Module IDs and contribution identities are case-sensitive;
identical metadata deduplicates, while conflicts or invalid schemas produce `DFG046`/`DFG047` and
are excluded before normalization.

Legacy manifests without a module declaration remain supported. Once a source marker is registered
as a module, capability checks apply to its contributions.

## Minimal request module

The following is representative output from a module analyzer; consumers should not maintain these
attributes by hand:

```csharp
using DomainFixture.Contracts;
using DomainFixture.Generation.Metadata;

[assembly: DomainFixtureModuleManifest(
    1,
    "Acme.DomainFixture.MediatR",
    "1.0.0",
    typeof(Acme.MediatRModule),
    new[] { DomainFixtureModuleCapabilities.Recipes })]

[assembly: DomainFixtureRecipeManifest(
    1,
    "Acme.DomainFixture.MediatR",
    typeof(Acme.MediatRModule),
    typeof(PlaceOrder),
    "Acme.Tests.Generated",
    "PlaceOrderFixture",
    "Valid")]
```

A MediatR integration can recognize `IRequest`/`IRequest<T>` semantically and emit one recipe
manifest per selected request. DomainFixture then owns value synthesis, transitive domain discovery,
factory generation, aliases, construction laws, coverage, and diagnostics. The core never needs a
MediatR reference or a hard-coded request convention.

## Validation module

A FluentValidation-style module has two responsibilities:

1. Translate validator rules into `DomainContractManifestAttribute` constraints using the module
   source marker.
2. Generate an `IFixtureValidator<T>` adapter and bind its deterministic type name with
   `DomainFixtureValidationManifestAttribute`.

```csharp
[assembly: DomainFixtureValidationManifest(
    1,
    "Acme.DomainFixture.FluentValidation",
    typeof(Acme.FluentValidationModule),
    typeof(PlaceOrder),
    "global::Acme.Generated.PlaceOrderValidationAdapter")]
```

The adapter owns all FluentValidation API calls and maps their result to DomainFixture's neutral
`ValidationReport`. Generated boundary tests instantiate only that adapter through
`IFixtureValidator<T>`. The core generator does not reference FluentValidation, discover its types,
or emit its API calls.

An explicit fixture validator remains authoritative over a module default. Likewise, an explicit
fixture recipe wins over an equivalent contributed recipe.

## Custom generation behavior

Metadata contributions use core-known, stable kind IDs so they can participate in normalization and
coverage. A module with genuinely new test semantics should not ask the core generator to load a
third-party provider DLL. Its analyzer can reference `DomainFixture.TestGenerator`, construct
`GeneratedTest`/`GeneratedTestSuite` models, and emit through the public test emitters during its own
generator pass. This preserves compiler isolation while still sharing DomainFixture's typed test
model, boundary generators, deterministic naming, and framework emitters.

In other words, manifests extend the shared domain specification; the TestGenerator SDK extends
source production. Both paths remain upfront and independently compilable.

## Package shape

A typical integration NuGet package contains:

```text
analyzers/dotnet/cs/Acme.DomainFixture.MediatR.dll
buildTransitive/Acme.DomainFixture.MediatR.props   (optional configuration)
```

The analyzer targets a Roslyn-compatible `netstandard` framework and references
`DomainFixture.Contracts` for metadata shapes. Framework packages should remain private to the
module implementation. Do not place runtime module discovery or service registration in the core
package.

## Visibility and debugging

Every accepted module build emits `DomainFixture.ModuleCatalog.g.cs`. It lists module IDs, versions,
capabilities, recipe roots, validation adapters, and contribution counts next to the existing domain
specification and coverage generated files. Module marker types are attribution only and are never
loaded or instantiated.
