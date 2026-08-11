namespace DomainFixture.TestGenerator.Modules;

public enum CompileTimeModuleCapabilityStatus
{
    Unregistered,
    Supported,
    MissingCapability,
    AmbiguousSource
}

public sealed class CompileTimeModuleCapabilityResolution
{
    public CompileTimeModuleCapabilityStatus Status { get; }
    public CompileTimeModuleDescriptor? Module { get; }
    public string SourceTypeName { get; }
    public string Capability { get; }

    private CompileTimeModuleCapabilityResolution(
        CompileTimeModuleCapabilityStatus status,
        CompileTimeModuleDescriptor? module,
        string sourceTypeName,
        string capability)
    {
        Status = status;
        Module = module;
        SourceTypeName = sourceTypeName;
        Capability = capability;
    }

    public static CompileTimeModuleCapabilityResolution Unregistered(
        string sourceTypeName,
        string capability) =>
        new(CompileTimeModuleCapabilityStatus.Unregistered, null, sourceTypeName, capability);

    public static CompileTimeModuleCapabilityResolution Supported(
        CompileTimeModuleDescriptor module,
        string capability) =>
        new(
            CompileTimeModuleCapabilityStatus.Supported,
            module,
            module.SourceTypeName,
            capability);

    public static CompileTimeModuleCapabilityResolution Missing(
        CompileTimeModuleDescriptor module,
        string capability) =>
        new(
            CompileTimeModuleCapabilityStatus.MissingCapability,
            module,
            module.SourceTypeName,
            capability);

    public static CompileTimeModuleCapabilityResolution Ambiguous(
        string sourceTypeName,
        string capability) =>
        new(CompileTimeModuleCapabilityStatus.AmbiguousSource, null, sourceTypeName, capability);
}
