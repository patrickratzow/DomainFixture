# Visible generated `.Domain` tests

The `.verified.cs` files are the exact NUnit sources generated from:

- `DomainFixture.Tests.Domain.Entities.RegistrationRequestValidator` in the real `.Domain` assembly;
- `DomainFixtureProfile.cs` in the test project;
- `RegistrationRequestFixture.cs` in the test project.
- `DomainFixture.Tests.Domain.ValueObjects.UsernameValidator` in the real `.Domain` assembly;
- `UsernameFixture.cs` and its centrally configured reconstruction strategy.
- `QualifiedNameFixture.cs`, which proves that reconstruction preserves the untouched property of
  a two-property immutable value object.

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
