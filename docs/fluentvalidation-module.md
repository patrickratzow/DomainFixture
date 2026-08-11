# FluentValidation module

`DomainFixture.Modules.FluentValidation` is a standalone Roslyn analyzer. It owns every
FluentValidation-specific operation used by the module path:

- semantic discovery of `AbstractValidator<T>` / `IValidator<T>`;
- conversion of supported rule chains into neutral `DomainContractManifestAttribute` constraints;
- generation of an `IFixtureValidator<T>` adapter for each validated subject;
- module and validation-binding manifests consumed by the downstream DomainFixture source generator.

The core source generator does not load the module or call FluentValidation. Generated tests invoke
the module-owned adapter through `DomainFixture.Validation.IFixtureValidator<T>`.

## Project wiring

Install the module analyzer in the project that contains the validators:

```xml
<ItemGroup>
  <PackageReference Include="FluentValidation" Version="10.3.6" />
  <PackageReference Include="DomainFixture" Version="..." />
  <PackageReference Include="DomainFixture.Modules.FluentValidation"
                    Version="..."
                    PrivateAssets="all" />
</ItemGroup>
```

For local project references, the equivalent is:

```xml
<ProjectReference Include="..\DomainFixture.Modules.FluentValidation\DomainFixture.Modules.FluentValidation.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

The downstream test project references the validator/domain assembly and the normal DomainFixture
source generator. It does not need `.UseFluentValidation()` or `RulesFrom<TValidator>()` for the
module default:

```csharp
public sealed class UsernameFixture : IFixtureTestConfiguration<Username>
{
    public void Configure(IFixtureTestBuilder<Username> fixture) =>
        fixture.Recipe("Validation").Baseline(Baseline);

    public static Username Baseline() => Username.From("baseline");
}
```

An explicit `ValidateWith(...)` declaration still overrides the contributed adapter.

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

`WithErrorCode("...")` is preserved when its argument is constant. Rules must originate from a
direct `RuleFor(subject => subject.Property)` chain. Unsupported rules produce `DFV001`; malformed
supported rules produce `DFV002`; unusable validator types produce `DFV003`; multiple validators for
one subject produce `DFV004`.

## Compilation lifecycle

The validator project compiles the module metadata and public adapter types into its assembly. The
test project then consumes those facts from the referenced assembly. This two-stage boundary is
required because Roslyn generators cannot inspect another generator's newly emitted source during
the same compilation.

The generated module marker is metadata attribution only. It is never instantiated. The generated
adapter is the only executable integration surface and contains the FluentValidation calls and
`ValidationResult` to `ValidationReport` mapping.
