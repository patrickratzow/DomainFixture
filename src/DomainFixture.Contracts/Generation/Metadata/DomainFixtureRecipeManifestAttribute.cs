using System;
using System.ComponentModel;

namespace DomainFixture.Generation.Metadata;

/// <summary>
/// Contributes a named synthesized recipe from an external compile-time module.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DomainFixtureRecipeManifestAttribute : Attribute
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public string ModuleId { get; }
    public Type SourceType { get; }
    public Type SubjectType { get; }
    public string ConfigurationNamespace { get; }
    public string ConfigurationName { get; }
    public string RecipeName { get; }

    public DomainFixtureRecipeManifestAttribute(
        int schemaVersion,
        string moduleId,
        Type sourceType,
        Type subjectType,
        string configurationNamespace,
        string configurationName,
        string recipeName)
    {
        SchemaVersion = schemaVersion;
        ModuleId = moduleId;
        SourceType = sourceType;
        SubjectType = subjectType;
        ConfigurationNamespace = configurationNamespace;
        ConfigurationName = configurationName;
        RecipeName = recipeName;
    }
}
