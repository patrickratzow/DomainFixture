using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Diagnostics;

internal static class GeneratorDiagnostics
{
    private static readonly DiagnosticDescriptor InvalidConfiguration = new(
        "DFG001",
        "Fixture-test configuration is incomplete",
        "Fixture-test configuration '{0}' must declare a recipe baseline using Baseline, Synthesize, or FromTransition, unless AutoSynthesizeRecipes is enabled",
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
        "Generated boundary test cannot mutate property",
        "Cannot generate the {2} boundary for '{0}.{1}': {3}",
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

    private static readonly DiagnosticDescriptor UnsupportedConstraintAdapter = new(
        "DFG015",
        "Domain constraint is not covered",
        "{0} rule '{1}' has no installed domain-constraint adapter; no boundary test was generated for it",
        "DomainFixture.Generation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UninterpretableConstraint = new(
        "DFG016",
        "Domain constraint could not be interpreted",
        "{0} rule '{1}' could not be converted into a domain constraint: {2}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingBoundaryProvider = new(
        "DFG017",
        "Domain constraint has no boundary provider",
        "Constraint '{0}' for '{1}.{2}' has no installed boundary-case provider",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedDomainContractSchema = new(
        "DFG018",
        "Domain contract schema is unsupported",
        "Domain contract '{1}' uses schema version {0}, which this generator cannot consume",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AmbiguousBoundaryProvider = new(
        "DFG019",
        "Domain constraint boundary provider is ambiguous",
        "Constraint '{0}' for '{1}.{2}' is handled by multiple providers: {3}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidBoundaryProvider = new(
        "DFG020",
        "Domain constraint boundary provider rejected its input",
        "Provider '{0}' could not generate constraint '{1}' for '{2}.{3}': {4}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedDomainScenarioSchema = new(
        "DFG021",
        "Domain scenario schema is unsupported",
        "Domain scenario '{1}' uses schema version {0}, which this generator cannot consume",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingScenarioProvider = new(
        "DFG022",
        "Domain scenario has no provider",
        "Scenario '{0}' for '{1}' has no installed scenario provider",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AmbiguousScenarioProvider = new(
        "DFG023",
        "Domain scenario provider is ambiguous",
        "Scenario '{0}' for '{1}' is handled by multiple providers: {2}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidScenarioProvider = new(
        "DFG024",
        "Domain scenario provider rejected its input",
        "Provider '{0}' could not generate scenario '{1}' for '{2}': {3}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AmbiguousOperationParameterMapping = new(
        "DFG025",
        "Construction operation parameter mapping is ambiguous",
        "Cannot infer which property of '{1}' receives parameter '{2}' from construction operation '{0}'",
        "DomainFixture.Generation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FixtureFactoryIdentifierCollision = new(
        "DFG026",
        "Generated fixture-factory scenario name is ambiguous",
        "Recipes '{1}' and '{2}' on fixture '{0}' produce the same generated scenario identifier",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedDomainOperationSchema = new(
        "DFG027",
        "Domain operation schema is unsupported",
        "Domain operation '{0}' uses an unsupported schema: {1}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidDomainOperationManifest = new(
        "DFG028",
        "Domain operation manifest is invalid",
        "Domain operation manifest '{0}' is invalid: {1}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingDomainOperation = new(
        "DFG029",
        "Domain operation metadata conflicts",
        "Domain operation '{0}' for '{1}' has conflicting signatures or parameter bindings",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedDomainOutcomeSchema = new(
        "DFG030",
        "Domain operation outcome schema is unsupported",
        "Domain operation outcome for '{0}' uses an unsupported schema: {1}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidDomainOutcomeManifest = new(
        "DFG031",
        "Domain operation outcome manifest is invalid",
        "Domain operation outcome for '{0}' is invalid or orphaned: {1}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingOperationRejection = new(
        "DFG032",
        "Operation rejection configuration conflicts",
        "Operation rejection configuration for '{0}' is duplicated or conflicting",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingOutcomeProvider = new(
        "DFG033",
        "Domain operation outcome has no provider",
        "Outcome '{0}' for operation '{1}' has no installed provider",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AmbiguousOutcomeProvider = new(
        "DFG034",
        "Domain operation outcome provider is ambiguous",
        "Outcome '{0}' for operation '{1}' is handled by multiple providers: {2}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidOutcomeProvider = new(
        "DFG035",
        "Domain operation outcome provider rejected its input",
        "Provider '{0}' could not generate outcome '{1}' for operation '{2}': {3}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingConfiguredOperation = new(
        "DFG036",
        "Configured rejection has no construction operation",
        "Rejection is configured for '{0}', but no mapped construction operation was discovered",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingConstrainedOperationParameter = new(
        "DFG037",
        "Construction constraints do not map to operation parameters",
        "Rejection is configured for operation '{0}', but none of its parameters map to a constrained member",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateTransition = new(
        "DFG038",
        "Domain transition is duplicated",
        "Fixture recipe '{0}' declares transition '{1}' more than once",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidTransitionOperation = new(
        "DFG039",
        "Domain transition operation is invalid",
        "Transition '{0}' must invoke one accessible, non-generic, parameterless synchronous instance method returning void",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidTransitionState = new(
        "DFG040",
        "Domain transition state is invalid",
        "Transition '{0}' has an invalid state selector or expected state: {1}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingTransitionIdentity = new(
        "DFG041",
        "Domain entity identity is missing or ambiguous",
        "Identity preservation is enabled for '{0}', but no unique readable 'Id' or '{1}Id' property was found",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingValidInstancePlan = new(
        "DFG042",
        "Valid fixture instance cannot be synthesized",
        "Recipe '{0}' for '{1}' has no valid instance plan: {2}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingOperationResult = new(
        "DFG043",
        "Generic result configuration conflicts",
        "Result wrapper '{1}' for subject '{0}' is configured more than once",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidConfiguredValue = new(
        "DFG044",
        "Configured fixture value is invalid",
        "Configured value for '{0}' must be a compile-time-emittable literal, static member, constructor, or static factory expression",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateConfiguredValue = new(
        "DFG045",
        "Configured fixture value conflicts",
        "Fixture value type '{0}' is configured more than once",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidCompileTimeModule = new(
        "DFG046",
        "Compile-time module metadata is invalid",
        "Compile-time module '{0}' is invalid: {1}",
        "DomainFixture.Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidCompileTimeModuleContribution = new(
        "DFG047",
        "Compile-time module contribution is invalid",
        "Compile-time module contribution from '{0}' is invalid: {1}",
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

    public static Diagnostic PropertyMutationUnavailable(
        Location? location,
        string subjectType,
        string propertyName,
        string ruleKind,
        string reason) =>
        Diagnostic.Create(
            InaccessibleProperty,
            location,
            subjectType,
            propertyName,
            ruleKind,
            reason);

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

    public static Diagnostic ConstraintAdapterMissing(
        Location? location,
        string adapterName,
        string ruleName) =>
        Diagnostic.Create(UnsupportedConstraintAdapter, location, adapterName, ruleName);

    public static Diagnostic ConstraintAdapterCouldNotInterpret(
        Location? location,
        string adapterName,
        string ruleName,
        string reason) =>
        Diagnostic.Create(UninterpretableConstraint, location, adapterName, ruleName, reason);

    public static Diagnostic BoundaryProviderMissing(
        Location? location,
        string constraintKind,
        string subjectType,
        string propertyName) =>
        Diagnostic.Create(
            MissingBoundaryProvider,
            location,
            constraintKind,
            subjectType,
            propertyName);

    public static Diagnostic DomainContractSchemaUnsupported(
        Location? location,
        int schemaVersion,
        string kindId) =>
        Diagnostic.Create(UnsupportedDomainContractSchema, location, schemaVersion, kindId);

    public static Diagnostic BoundaryProviderAmbiguous(
        Location? location,
        string kindId,
        string subjectType,
        string propertyName,
        string providerIds) =>
        Diagnostic.Create(
            AmbiguousBoundaryProvider,
            location,
            kindId,
            subjectType,
            propertyName,
            providerIds);

    public static Diagnostic BoundaryProviderInvalid(
        Location? location,
        string providerId,
        string kindId,
        string subjectType,
        string propertyName,
        string reason) =>
        Diagnostic.Create(
            InvalidBoundaryProvider,
            location,
            providerId,
            kindId,
            subjectType,
            propertyName,
            reason);

    public static Diagnostic DomainScenarioSchemaUnsupported(
        Location? location,
        int schemaVersion,
        string kindId) =>
        Diagnostic.Create(UnsupportedDomainScenarioSchema, location, schemaVersion, kindId);

    public static Diagnostic ScenarioProviderMissing(
        Location? location,
        string kindId,
        string subjectType) =>
        Diagnostic.Create(MissingScenarioProvider, location, kindId, subjectType);

    public static Diagnostic ScenarioProviderAmbiguous(
        Location? location,
        string kindId,
        string subjectType,
        string providerIds) =>
        Diagnostic.Create(
            AmbiguousScenarioProvider,
            location,
            kindId,
            subjectType,
            providerIds);

    public static Diagnostic ScenarioProviderInvalid(
        Location? location,
        string providerId,
        string kindId,
        string subjectType,
        string reason) =>
        Diagnostic.Create(
            InvalidScenarioProvider,
            location,
            providerId,
            kindId,
            subjectType,
            reason);

    public static Diagnostic OperationParameterMappingAmbiguous(
        Location? location,
        string operationName,
        string subjectType,
        string parameterName) =>
        Diagnostic.Create(
            AmbiguousOperationParameterMapping,
            location,
            operationName,
            subjectType,
            parameterName);

    public static Diagnostic FixtureFactoryScenarioCollision(
        Location? location,
        string fixtureName,
        string firstRecipe,
        string secondRecipe) =>
        Diagnostic.Create(
            FixtureFactoryIdentifierCollision,
            location,
            fixtureName,
            firstRecipe,
            secondRecipe);

    public static Diagnostic DomainOperationSchemaUnsupported(
        Location? location,
        string operationId,
        string reason) =>
        Diagnostic.Create(UnsupportedDomainOperationSchema, location, operationId, reason);

    public static Diagnostic DomainOperationManifestInvalid(
        Location? location,
        string operationId,
        string reason) =>
        Diagnostic.Create(InvalidDomainOperationManifest, location, operationId, reason);

    public static Diagnostic DomainOperationConflicting(
        Location? location,
        string operationId,
        string subjectType) =>
        Diagnostic.Create(ConflictingDomainOperation, location, operationId, subjectType);

    public static Diagnostic DomainOutcomeSchemaUnsupported(
        Location? location,
        string operationId,
        string reason) =>
        Diagnostic.Create(UnsupportedDomainOutcomeSchema, location, operationId, reason);

    public static Diagnostic DomainOutcomeManifestInvalid(
        Location? location,
        string operationId,
        string reason) =>
        Diagnostic.Create(InvalidDomainOutcomeManifest, location, operationId, reason);

    public static Diagnostic OperationRejectionConflicting(Location? location, string scope) =>
        Diagnostic.Create(ConflictingOperationRejection, location, scope);

    public static Diagnostic OutcomeProviderMissing(
        Location? location,
        string kindId,
        string operationId) =>
        Diagnostic.Create(MissingOutcomeProvider, location, kindId, operationId);

    public static Diagnostic OutcomeProviderAmbiguous(
        Location? location,
        string kindId,
        string operationId,
        string providers) =>
        Diagnostic.Create(AmbiguousOutcomeProvider, location, kindId, operationId, providers);

    public static Diagnostic OutcomeProviderInvalid(
        Location? location,
        string provider,
        string kindId,
        string operationId,
        string reason) =>
        Diagnostic.Create(InvalidOutcomeProvider, location, provider, kindId, operationId, reason);

    public static Diagnostic ConfiguredOperationMissing(Location? location, string subjectType) =>
        Diagnostic.Create(MissingConfiguredOperation, location, subjectType);

    public static Diagnostic ConstrainedOperationParameterMissing(
        Location? location,
        string operationId) =>
        Diagnostic.Create(MissingConstrainedOperationParameter, location, operationId);

    public static Diagnostic TransitionDuplicated(
        Location? location,
        string recipeName,
        string transitionName) =>
        Diagnostic.Create(DuplicateTransition, location, recipeName, transitionName);

    public static Diagnostic TransitionOperationInvalid(
        Location? location,
        string transitionName) =>
        Diagnostic.Create(InvalidTransitionOperation, location, transitionName);

    public static Diagnostic TransitionStateInvalid(
        Location? location,
        string transitionName,
        string reason) =>
        Diagnostic.Create(InvalidTransitionState, location, transitionName, reason);

    public static Diagnostic TransitionIdentityMissing(
        Location? location,
        string subjectType,
        string subjectShortName) =>
        Diagnostic.Create(
            MissingTransitionIdentity,
            location,
            subjectType,
            subjectShortName);

    public static Diagnostic ValidInstancePlanMissing(
        Location? location,
        string recipeName,
        string subjectType,
        string reason) =>
        Diagnostic.Create(
            MissingValidInstancePlan,
            location,
            recipeName,
            subjectType,
            reason);

    public static Diagnostic OperationResultConflicting(
        Location? location,
        string subjectType,
        string resultType) =>
        Diagnostic.Create(
            ConflictingOperationResult,
            location,
            subjectType,
            resultType);

    public static Diagnostic ConfiguredValueInvalid(Location? location, string typeName) =>
        Diagnostic.Create(InvalidConfiguredValue, location, typeName);

    public static Diagnostic ConfiguredValueDuplicated(Location? location, string typeName) =>
        Diagnostic.Create(DuplicateConfiguredValue, location, typeName);

    public static Diagnostic CompileTimeModuleInvalid(
        Location? location,
        string moduleId,
        string reason) =>
        Diagnostic.Create(InvalidCompileTimeModule, location, moduleId, reason);

    public static Diagnostic CompileTimeModuleContributionInvalid(
        Location? location,
        string moduleId,
        string reason) =>
        Diagnostic.Create(
            InvalidCompileTimeModuleContribution,
            location,
            moduleId,
            reason);
}
