using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Diagnostics;

internal static class GeneratorDiagnostics
{
    private static readonly DiagnosticDescriptor InvalidConfiguration = new(
        "DFG001",
        "Fixture-test configuration is incomplete",
        "Fixture-test configuration '{0}' must declare a fluent chain containing Recipe, Baseline, ValidateWith, and RulesFrom",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidFactoryMethod = new(
        "DFG002",
        "Fixture factory method is inaccessible",
        "Factory method '{0}' must be static and accessible from generated tests",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidValidatorFactory = new(
        "DFG003",
        "Validator factory is incompatible",
        "ValidateWith factory on '{0}' must return an IFixtureValidator<{1}>",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NoSupportedRules = new(
        "DFG004",
        "No supported validation rules were found",
        "Validation rules type '{0}' contains no supported FluentValidation Length(minimum, maximum) rules",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InaccessibleProperty = new(
        "DFG005",
        "Generated test cannot mutate property",
        "Property '{0}' must have an accessible setter for generated boundary tests",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateRecipe = new(
        "DFG006",
        "Fixture recipe is duplicated",
        "Fixture-test configuration '{0}' declares recipe '{1}' more than once",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic IncompleteConfiguration(Location? location, string configurationName) =>
        Diagnostic.Create(InvalidConfiguration, location, configurationName);

    public static Diagnostic InaccessibleFactory(Location? location, string factoryName) =>
        Diagnostic.Create(InvalidFactoryMethod, location, factoryName);

    public static Diagnostic IncompatibleValidator(
        Location? location,
        string configurationName,
        string subjectType) =>
        Diagnostic.Create(InvalidValidatorFactory, location, configurationName, subjectType);

    public static Diagnostic MissingSupportedRules(Location? location, string validationRulesType) =>
        Diagnostic.Create(NoSupportedRules, location, validationRulesType);

    public static Diagnostic PropertySetterInaccessible(Location? location, string propertyName) =>
        Diagnostic.Create(InaccessibleProperty, location, propertyName);

    public static Diagnostic RecipeDuplicated(
        Location? location,
        string configurationName,
        string recipeName) =>
        Diagnostic.Create(DuplicateRecipe, location, configurationName, recipeName);
}
