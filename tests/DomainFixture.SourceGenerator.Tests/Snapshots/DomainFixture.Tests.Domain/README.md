# Visible generated `.Domain` tests

The `.verified.cs` files are the exact NUnit sources generated from:

- `DomainFixture.Tests.Domain.Entities.RegistrationRequestValidator` in the real `.Domain` assembly;
- `DomainFixtureProfile.cs` in the test project;
- `RegistrationRequestFixture.cs` in the test project.
- `DomainFixture.Tests.Domain.ValueObjects.UsernameValidator` in the real `.Domain` assembly;
- `UsernameFixture.cs` and its centrally configured reconstruction strategy.

Run the snapshot check with:

```powershell
dotnet test tests/DomainFixture.SourceGenerator.Tests/DomainFixture.SourceGenerator.Tests.csproj `
  --filter FullyQualifiedName~DomainGeneratedSuiteSnapshotTests
```

When generation changes, the test reports the first mismatched line and prints the complete newly
generated source. Review that output and update the `.verified.cs` file only when the change is
intentional.
