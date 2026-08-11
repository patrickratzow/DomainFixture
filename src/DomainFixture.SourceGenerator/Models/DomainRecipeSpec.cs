namespace DomainFixture.SourceGenerator.Models;

internal sealed class DomainRecipeSpec
{
    public FixtureGenerationSpec Configuration { get; }

    public string Name => Configuration.RecipeName;
    public string SourceId => $"{Configuration.ConfigurationName}:{Configuration.RecipeName}";

    public DomainRecipeSpec(FixtureGenerationSpec configuration)
    {
        Configuration = configuration;
    }
}
