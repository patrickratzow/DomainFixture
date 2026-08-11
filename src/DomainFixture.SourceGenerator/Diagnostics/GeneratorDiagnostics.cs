using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Diagnostics;

internal static class GeneratorDiagnostics
{
    private static readonly DiagnosticDescriptor InvalidConfiguration = new(
        "DFG001",
        "Fixture-test configuration is incomplete",
        "Fixture-test configuration '{0}' must declare a fluent chain containing Recipe and Baseline",
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
        "Validation rules type '{0}' produced no rules supported by the installed rule providers",
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

    private static readonly DiagnosticDescriptor DuplicateGenerationProfile = new(
        "DFG007",
        "Generation profile is duplicated",
        "Assembly declares more than one IFixtureGenerationProfile; keep one central generation profile",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidGenerationProfile = new(
        "DFG008",
        "Generation profile is invalid",
        "Generation profile '{0}' must implement Configure with supported fluent configuration calls",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingActivation = new(
        "DFG009",
        "Generation activation is ambiguous",
        "Generation profile '{0}' selects both factory and service-provider activation",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidPropertyMutation = new(
        "DFG010",
        "Property reconstruction is invalid",
        "Property reconstruction in generation profile '{0}' must select a property and an accessible static reconstruction method",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicatePropertyMutation = new(
        "DFG011",
        "Property reconstruction is duplicated",
        "Generation profile '{0}' configures reconstruction for '{1}.{2}' more than once",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingSubjectValidator = new(
        "DFG012",
        "No validation rules were found for subject",
        "No FluentValidation validator with supported rules was found for subject '{0}'",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AmbiguousSubjectValidator = new(
        "DFG013",
        "Validation rules are ambiguous",
        "More than one FluentValidation validator was found for subject '{0}'; select one with RulesFrom<TValidator>()",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingValidationExecution = new(
        "DFG014",
        "Validation execution is not configured",
        "Fixture '{0}' must use ValidateWith(...) or enable FluentValidation in the assembly generation profile",
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

    public static Diagnostic GenerationProfileDuplicated(Location? location) =>
        Diagnostic.Create(DuplicateGenerationProfile, location);

    public static Diagnostic GenerationProfileInvalid(Location? location, string profileName) =>
        Diagnostic.Create(InvalidGenerationProfile, location, profileName);

    public static Diagnostic GenerationActivationConflicting(Location? location, string profileName) =>
        Diagnostic.Create(ConflictingActivation, location, profileName);

    public static Diagnostic PropertyMutationInvalid(Location? location, string profileName) =>
        Diagnostic.Create(InvalidPropertyMutation, location, profileName);

    public static Diagnostic PropertyMutationDuplicated(
        Location? location,
        string profileName,
        string subjectType,
        string propertyName) =>
        Diagnostic.Create(
            DuplicatePropertyMutation,
            location,
            profileName,
            subjectType,
            propertyName);

    public static Diagnostic SubjectValidatorMissing(Location? location, string subjectType) =>
        Diagnostic.Create(MissingSubjectValidator, location, subjectType);

    public static Diagnostic SubjectValidatorAmbiguous(Location? location, string subjectType) =>
        Diagnostic.Create(AmbiguousSubjectValidator, location, subjectType);

    public static Diagnostic ValidationExecutionMissing(Location? location, string fixtureName) =>
        Diagnostic.Create(MissingValidationExecution, location, fixtureName);
}
