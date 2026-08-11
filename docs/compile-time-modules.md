# Compile-time modules

DomainFixture modules extend discovery without runtime plugin loading, reflection, or dependency
injection. First-party modules are separate libraries composed through the internal pipeline of the
single `DomainFixtureIncrementalGenerator` entry point. Module libraries contain no `[Generator]`
classes. They translate framework-specific symbols into neutral contribution streams and versioned
assembly metadata.

```text
producer compilation                     test compilation
--------------------                     ----------------
FluentValidation / MediatR symbols
        |
original generator + internal module pipeline
        |
module + contribution manifests  --->    DomainFixture source generator
                                         |
                                         normalized DomainTypeSpec
                                         |
                                         factories + generated tests
```

The internal pipeline supplies same-compilation facts directly to the central generator.
Contribution manifests are also compiled into the producer assembly for consumption by a
downstream test project. This second representation remains necessary because Roslyn generators
cannot semantically inspect their own newly emitted source during the current compilation.

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

The internal [FluentValidation module](fluentvalidation-module.md) uses this path today.

The FluentValidation module has two responsibilities:

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
`IFixtureValidator<T>`. The generator assembly has no FluentValidation package reference. Its
internal module recognizes FluentValidation symbols semantically and owns all emitted framework
API calls.

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

## External module escape hatch

An independently authored module cannot be injected into an already compiled generator pipeline.
External integrations therefore retain the metadata-producer escape hatch. A typical external
integration NuGet package contains:

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
