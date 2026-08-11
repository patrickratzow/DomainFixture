namespace DomainFixture.TestGenerator.Modules;

/// <summary>
/// Binds module-owned constraint metadata to a generated runtime validation adapter.
/// </summary>
public sealed class CompileTimeValidationContribution
{
    public int SchemaVersion { get; }
    public string ModuleId { get; }
    public string SourceTypeName { get; }
    public string SubjectTypeName { get; }
    public string AdapterTypeName { get; }

    public CompileTimeValidationContribution(
        int schemaVersion,
        string moduleId,
        string sourceTypeName,
        string subjectTypeName,
        string adapterTypeName)
    {
        SchemaVersion = schemaVersion;
        ModuleId = moduleId ?? string.Empty;
        SourceTypeName = sourceTypeName ?? string.Empty;
        SubjectTypeName = subjectTypeName ?? string.Empty;
        AdapterTypeName = adapterTypeName ?? string.Empty;
    }
}
