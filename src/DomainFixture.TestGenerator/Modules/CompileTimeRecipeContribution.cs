namespace DomainFixture.TestGenerator.Modules;

/// <summary>
/// A module-owned recipe root. The source generator turns this into the same normalized recipe
/// model used by handwritten fixture configurations.
/// </summary>
public sealed class CompileTimeRecipeContribution
{
    public int SchemaVersion { get; }
    public string ModuleId { get; }
    public string SourceTypeName { get; }
    public string SubjectTypeName { get; }
    public string ConfigurationNamespace { get; }
    public string ConfigurationName { get; }
    public string RecipeName { get; }

    public CompileTimeRecipeContribution(
        int schemaVersion,
        string moduleId,
        string sourceTypeName,
        string subjectTypeName,
        string configurationNamespace,
        string configurationName,
        string recipeName)
    {
        SchemaVersion = schemaVersion;
        ModuleId = moduleId ?? string.Empty;
        SourceTypeName = sourceTypeName ?? string.Empty;
        SubjectTypeName = subjectTypeName ?? string.Empty;
        ConfigurationNamespace = configurationNamespace ?? string.Empty;
        ConfigurationName = configurationName ?? string.Empty;
        RecipeName = recipeName ?? string.Empty;
    }
}
