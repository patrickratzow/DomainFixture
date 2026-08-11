namespace DomainFixture.TestGenerator.Modules;

public enum CompileTimeModuleIssueKind
{
    UnsupportedModuleSchema,
    InvalidModule,
    ConflictingModule,
    UnsupportedRecipeSchema,
    InvalidRecipe,
    UnknownModule,
    UnsupportedCapability,
    SourceMismatch,
    ConflictingRecipe,
    UnsupportedValidationSchema,
    InvalidValidation,
    ConflictingValidation
}

public sealed class CompileTimeModuleIssue
{
    public CompileTimeModuleIssueKind Kind { get; }
    public string ModuleId { get; }
    public string Message { get; }

    public CompileTimeModuleIssue(
        CompileTimeModuleIssueKind kind,
        string moduleId,
        string message)
    {
        Kind = kind;
        ModuleId = moduleId;
        Message = message;
    }
}
