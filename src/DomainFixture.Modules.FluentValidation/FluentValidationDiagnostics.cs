using Microsoft.CodeAnalysis;

namespace DomainFixture.Modules.FluentValidation;

internal static class FluentValidationDiagnostics
{
    private const string Category = "DomainFixture.FluentValidation";

    public static readonly DiagnosticDescriptor UnsupportedRule = new(
        "DFV001",
        "FluentValidation rule is not supported by the module",
        "FluentValidation rule '{0}' has no DomainFixture constraint adapter",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UninterpretableRule = new(
        "DFV002",
        "FluentValidation rule could not be interpreted",
        "FluentValidation rule '{0}' could not be converted into a DomainFixture constraint: {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidValidator = new(
        "DFV003",
        "FluentValidation validator cannot be adapted",
        "FluentValidation validator '{0}' cannot be adapted: {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousValidator = new(
        "DFV004",
        "Multiple validators target one subject",
        "Multiple FluentValidation validators target '{0}': {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
