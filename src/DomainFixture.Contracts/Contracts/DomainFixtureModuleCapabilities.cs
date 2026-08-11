namespace DomainFixture.Contracts;

/// <summary>
/// Well-known compile-time contribution families understood by DomainFixture generators.
/// Module packages may declare additional capability IDs for their own tooling.
/// </summary>
public static class DomainFixtureModuleCapabilities
{
    public const string Constraints = "domainfixture.module.constraints";
    public const string Scenarios = "domainfixture.module.scenarios";
    public const string Operations = "domainfixture.module.operations";
    public const string OperationOutcomes = "domainfixture.module.operation-outcomes";
    public const string Recipes = "domainfixture.module.recipes";
    public const string Validators = "domainfixture.module.validators";
}
