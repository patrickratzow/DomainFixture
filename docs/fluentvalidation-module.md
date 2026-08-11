# FluentValidation module

FluentValidation support lives in the separate `DomainFixture.Modules.FluentValidation` library
and is composed into the original `DomainFixture.SourceGenerator`. The module library contains no
`[Generator]` entry point; `DomainFixtureIncrementalGenerator` is the only Roslyn generator.

The internal module owns:

- semantic discovery of `AbstractValidator<T>` / `IValidator<T>`;
- same-compilation constraint contributions to the central generator pipeline;
- conversion of supported rules into neutral `DomainContractManifestAttribute` constraints;
- generation of one `IFixtureValidator<T>` adapter per validated subject;
- module and validation-binding metadata for a downstream test compilation.

The central generation pipeline remains framework-neutral. FluentValidation API calls exist only
inside the module-owned adapter emitted into the validator assembly.

## Project wiring

The project containing validators references FluentValidation, DomainFixture, and the normal
DomainFixture source-generator distribution. Packaged distributions place the FluentValidation
module dependency beside the main analyzer DLL; no second generator is installed:

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation" Version="10.3.6" />
  <PackageReference Include="DomainFixture" Version="..." />
  <PackageReference Include="DomainFixture.SourceGenerator" Version="..." PrivateAssets="all" />
</ItemGroup>
```

For repository project references, the generator is the same analyzer used everywhere else:

```xml
<ProjectReference Include="..\DomainFixture.SourceGenerator\DomainFixture.SourceGenerator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
<ProjectReference Include="..\DomainFixture.Modules.FluentValidation\DomainFixture.Modules.FluentValidation.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

The second local reference only makes the generator's library dependency available in Roslyn's
analyzer load context. It has no generator entry point and therefore does not execute independently.

The test project references the compiled validator/domain assembly and the same source generator.
It does not need `.UseFluentValidation()` or `RulesFrom<TValidator>()`:

```csharp
public sealed class UsernameFixture : IFixtureTestConfiguration<Username>
{
    public void Configure(IFixtureTestBuilder<Username> fixture) =>
        fixture.Recipe("Validation").Baseline(Baseline);

    public static Username Baseline() => Username.From("baseline");
}
```

An explicit `ValidateWith(...)` declaration remains authoritative.

## Supported rules

The initial module supports constant, direct-property rules:

| FluentValidation rule | DomainFixture constraint |
| --- | --- |
| `NotNull()` | `domainfixture.text.not-null` |
| `NotEmpty()` | `domainfixture.text.not-empty` |
| `Length(min, max)` | `domainfixture.text.length` |
| `MinimumLength(min)` | `domainfixture.text.minimum-length` |
| `MaximumLength(max)` | `domainfixture.text.maximum-length` |
| `InclusiveBetween(min, max)` | `domainfixture.int32.inclusive-range` |
| `ExclusiveBetween(min, max)` | `domainfixture.int32.exclusive-range` |
| `GreaterThan(min)` | `domainfixture.int32.greater-than` |
| `LessThan(max)` | `domainfixture.int32.less-than` |

`WithErrorCode("...")` is preserved when constant. Rules must originate from a direct
`RuleFor(subject => subject.Property)` chain. Unsupported rules produce `DFV001`; malformed rules
produce `DFV002`; unusable validator types produce `DFV003`; multiple validators for one subject
produce `DFV004`.

## Compilation lifecycle

During the validator/domain compilation, `DomainFixtureIncrementalGenerator` invokes the internal
FluentValidation module. The module contributes constraints directly for same-compilation work and
emits public adapters plus neutral manifests into the domain assembly.

During the test compilation, another invocation of that same generator reads the referenced
assembly manifests and generates boundary tests. The two-stage metadata boundary remains necessary
because a generator cannot semantically inspect source it emitted earlier in its own compilation.

Adapter names use deterministic 64-bit FNV-1a suffixes. The generated module marker is attribution
metadata only and is never instantiated.
