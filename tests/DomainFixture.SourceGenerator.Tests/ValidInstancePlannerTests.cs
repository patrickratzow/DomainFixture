using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Generation;
using DomainFixture.SourceGenerator.Models;
using FluentAssertions;
using NUnit.Framework;

namespace DomainFixture.SourceGenerator.Tests;

[TestFixture]
public sealed class ValidInstancePlannerTests
{
    [Test]
    public void Planner_ShouldPreferSelectedRecipeBaselineOverConstructionSynthesis()
    {
        var recipe = new DomainRecipeSpec(CreateFixtureConfiguration());
        var operation = Operation(
            DomainOperationKinds.StaticFactory,
            new DomainOperationParameterContract(
                "name",
                "global::System.String",
                "Name"));
        var type = TypeSpec(
            ImmutableArray.Create(recipe),
            ImmutableArray.Create(Fact(operation)));

        var result = ValidInstanceProviderPipeline.Resolve(
            new ValidInstancePlanningRequest(type, recipe));

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be(
            "global::Example.PersonFixture.Baseline()");
        result.Plan.ChosenOperation.Should().BeNull();
        result.Plan.Provenance.Should().Be(ValidInstanceProvenance.RecipeBaseline);
        result.Plan.SourceId.Should().Be("PersonFixture:Valid");
    }

    [Test]
    public void Planner_ShouldSynthesizeMappedFactoryArgumentsAndRecordProvenance()
    {
        var operation = Operation(
            DomainOperationKinds.StaticFactory,
            new DomainOperationParameterContract(
                "name",
                "global::System.String",
                "Name"),
            new DomainOperationParameterContract(
                "age",
                "global::System.Int32",
                "Age"));
        var type = TypeSpec(
            ImmutableArray<DomainRecipeSpec>.Empty,
            ImmutableArray.Create(Fact(
                operation,
                DomainFactProvenance.Manifest,
                "external-person-adapter")),
            Fact(Constraint(
                DomainConstraintKinds.TextMinimumLength,
                minimum: 2,
                memberPath: "Name")),
            Fact(Constraint(
                DomainConstraintKinds.Int32GreaterThan,
                minimum: 17,
                memberPath: "Age")));

        var result = ValidInstanceProviderPipeline.Resolve(
            new ValidInstancePlanningRequest(type));

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be(
            "global::Example.Person.From(\"aa\", 18)");
        result.Plan.ChosenOperation.Should().BeSameAs(operation);
        result.Plan.SourceId.Should().Be("external-person-adapter");
        result.Plan.FactProvenance.Should().Be(DomainFactProvenance.Manifest);
        result.Plan.Parameters.Select(parameter => parameter.Provenance).Should().Equal(
            ValidInstanceProvenance.StringConstraint,
            ValidInstanceProvenance.Int32Constraint);
    }

    [Test]
    public void Planner_ShouldExplainEveryUncoveredConstructionParameter()
    {
        var operation = Operation(
            DomainOperationKinds.Constructor,
            new DomainOperationParameterContract(
                "status",
                "global::Example.RegistrationStatus",
                "Status"));
        var type = TypeSpec(
            ImmutableArray<DomainRecipeSpec>.Empty,
            ImmutableArray.Create(Fact(operation)));

        var result = ValidInstanceProviderPipeline.Resolve(
            new ValidInstancePlanningRequest(type));

        result.IsCovered.Should().BeFalse();
        result.UncoveredReasons.Should().ContainSingle()
            .Which.Should().Contain(operation.OperationId)
            .And.Contain("parameter 'status'")
            .And.Contain("no nested recipe factory");
    }

    [Test]
    public void StringProvider_ShouldChooseValueInsideAllConstraints()
    {
        var result = ResolveValue(
            "global::System.String",
            Constraint(
                DomainConstraintKinds.TextMinimumLength,
                minimum: 3),
            Constraint(
                DomainConstraintKinds.TextMaximumLength,
                maximum: 5),
            Constraint(DomainConstraintKinds.TextNotEmpty));

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be("\"aaa\"");
        result.Plan.Provenance.Should().Be(ValidInstanceProvenance.StringConstraint);
    }

    [Test]
    public void StringProvider_ShouldExplainIncompatibleConstraints()
    {
        var result = ResolveValue(
            "string",
            Constraint(DomainConstraintKinds.TextMinimumLength, minimum: 4),
            Constraint(DomainConstraintKinds.TextMaximumLength, maximum: 2));

        result.IsCovered.Should().BeFalse();
        result.ProviderId.Should().Be("domainfixture.valid-values.string");
        result.UncoveredReason.Should().Contain("no valid length interval");
    }

    [Test]
    public void Int32Provider_ShouldChooseValueInsideExclusiveRange()
    {
        var result = ResolveValue(
            "global::System.Int32",
            Constraint(
                DomainConstraintKinds.Int32ExclusiveRange,
                minimum: 10,
                maximum: 14));

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be("11");
        result.Plan.Provenance.Should().Be(ValidInstanceProvenance.Int32Constraint);
    }

    [TestCase("bool", "true", ValidInstanceProvenance.PrimitiveDefault)]
    [TestCase("global::System.Int32?", "0", ValidInstanceProvenance.PrimitiveDefault)]
    [TestCase(
        "global::System.Guid",
        "new global::System.Guid(\"00000000-0000-0000-0000-000000000001\")",
        ValidInstanceProvenance.DeterministicGuid)]
    public void PrimitiveProviders_ShouldBeDeterministic(
        string typeName,
        string expression,
        string provenance)
    {
        var result = ResolveValue(typeName);

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be(expression);
        result.Plan.Provenance.Should().Be(provenance);
    }

    [Test]
    public void ConfiguredValue_ShouldOverrideBuiltInAndCoverEnum()
    {
        var configured = new[]
        {
            new ConfiguredValueSpec("global::System.Boolean", "false", null),
            new ConfiguredValueSpec(
                "global::Example.RegistrationStatus",
                "global::Example.RegistrationStatus.Pending",
                null)
        };

        var boolean = ResolveValueWithConfigured("bool", configured);
        var status = ResolveValueWithConfigured(
            "global::Example.RegistrationStatus?",
            configured);

        boolean.Plan!.Expression.Should().Be("false");
        boolean.Plan.Provenance.Should().Be(ValidInstanceProvenance.ConfiguredValue);
        status.Plan!.Expression.Should().Be("global::Example.RegistrationStatus.Pending");
        status.Plan.Provenance.Should().Be(ValidInstanceProvenance.ConfiguredValue);
    }

    [Test]
    public void NestedProvider_ShouldUseAnotherRecipeFactoryExpression()
    {
        var result = ResolveValue(
            "global::Example.Address",
            nestedResolver: typeName => typeName == "global::Example.Address"
                ? NestedValidInstanceResolution.Covered(
                    "global::Example.AddressFixtureFactory.Valid.Create()")
                : NestedValidInstanceResolution.Uncovered("not registered"));

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be(
            "global::Example.AddressFixtureFactory.Valid.Create()");
        result.Plan.Provenance.Should().Be(ValidInstanceProvenance.NestedRecipe);
    }

    [TestCase(
        "global::Example.Address[]",
        "new global::Example.Address[] { global::Example.AddressFixtureFactory.Valid.Create() }")]
    [TestCase(
        "global::System.Collections.Generic.IEnumerable<global::Example.Address>",
        "new global::Example.Address[] { global::Example.AddressFixtureFactory.Valid.Create() }")]
    [TestCase(
        "global::System.Collections.Generic.IReadOnlyCollection<global::Example.Address>",
        "new global::Example.Address[] { global::Example.AddressFixtureFactory.Valid.Create() }")]
    [TestCase(
        "global::System.Collections.Generic.IReadOnlyList<global::Example.Address>",
        "new global::Example.Address[] { global::Example.AddressFixtureFactory.Valid.Create() }")]
    [TestCase(
        "global::System.Collections.Generic.List<global::Example.Address>",
        "new global::System.Collections.Generic.List<global::Example.Address> { global::Example.AddressFixtureFactory.Valid.Create() }")]
    public void CollectionProvider_ShouldUseAvailableElementFactory(
        string typeName,
        string expectedExpression)
    {
        var result = ResolveValue(
            typeName,
            nestedResolver: elementType => elementType == "global::Example.Address"
                ? NestedValidInstanceResolution.Covered(
                    "global::Example.AddressFixtureFactory.Valid.Create()")
                : NestedValidInstanceResolution.Uncovered("not registered"));

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be(expectedExpression);
        result.Plan.Provenance.Should().Be(ValidInstanceProvenance.Collection);
    }

    [Test]
    public void CollectionProvider_ShouldExplainMissingElementFactory()
    {
        var result = ResolveValue(
            "global::System.Collections.Generic.IReadOnlyList<global::Example.Address>");

        result.IsCovered.Should().BeFalse();
        result.ProviderId.Should().Be("domainfixture.valid-values.collection");
        result.UncoveredReason.Should().Contain("collection element")
            .And.Contain("no nested recipe factory");
    }

    [TestCase(
        "global::System.Collections.Generic.IReadOnlyList<string>",
        "new string[] { \"a\" }")]
    [TestCase(
        "global::System.Collections.Generic.HashSet<int>",
        "new global::System.Collections.Generic.HashSet<int> { 0 }")]
    public void CollectionProvider_ShouldRecursivelyResolvePrimitiveElements(
        string typeName,
        string expectedExpression)
    {
        var result = ResolveValue(typeName);

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be(expectedExpression);
    }

    [Test]
    public void DictionaryProvider_ShouldResolvePrimitiveKeyAndNestedValue()
    {
        var result = ResolveValue(
            "global::System.Collections.Generic.IReadOnlyDictionary<string,global::Example.Address>",
            nestedResolver: typeName => typeName == "global::Example.Address"
                ? NestedValidInstanceResolution.Covered("global::Example.AddressFixtureFactory.Valid.Create()")
                : NestedValidInstanceResolution.Uncovered("not registered"));

        result.IsCovered.Should().BeTrue();
        result.Plan!.Expression.Should().Be(
            "new global::System.Collections.Generic.Dictionary<string, global::Example.Address> { [\"a\"] = global::Example.AddressFixtureFactory.Valid.Create() }");
    }

    [Test]
    public void Enum_ShouldRemainExplicitlyUncoveredUntilEnumProviderIsAdded()
    {
        var result = ResolveValue("global::Example.RegistrationStatus");

        result.IsCovered.Should().BeFalse();
        result.ProviderId.Should().Be("domainfixture.valid-values.nested-recipe");
        result.UncoveredReason.Should().Contain("no nested recipe factory");
    }

    private static ValidInstanceValuePlanningResult ResolveValue(
        string typeName,
        params DomainConstraintContract[] constraints) =>
        ResolveValueCore(typeName, null, null, constraints);

    private static ValidInstanceValuePlanningResult ResolveValueWithConfigured(
        string typeName,
        IReadOnlyList<ConfiguredValueSpec> configuredValues,
        params DomainConstraintContract[] constraints) =>
        ResolveValueCore(typeName, null, configuredValues, constraints);

    private static ValidInstanceValuePlanningResult ResolveValue(
        string typeName,
        System.Func<string, NestedValidInstanceResolution>? nestedResolver,
        params DomainConstraintContract[] constraints)
        => ResolveValueCore(typeName, nestedResolver, null, constraints);

    private static ValidInstanceValuePlanningResult ResolveValueCore(
        string typeName,
        System.Func<string, NestedValidInstanceResolution>? nestedResolver,
        IReadOnlyList<ConfiguredValueSpec>? configuredValues,
        params DomainConstraintContract[] constraints)
    {
        return ValidInstanceValueProviderPipeline.Resolve(
            new ValidInstanceValuePlanningRequest(
                new DomainOperationParameterContract("value", typeName, "Value"),
                constraints,
                nestedResolver,
                configuredValues));
    }

    private static DomainConstraintContract Constraint(
        string kind,
        int? minimum = null,
        int? maximum = null,
        string memberPath = "Value")
    {
        var parameters = new Dictionary<string, string>();
        if (minimum is not null)
            parameters[DomainConstraintParameters.Minimum] = minimum.Value.ToString();
        if (maximum is not null)
            parameters[DomainConstraintParameters.Maximum] = maximum.Value.ToString();

        return new DomainConstraintContract(
            kind,
            "global::Example.Validator",
            "global::Example.Subject",
            memberPath,
            parameters);
    }

    private static DomainOperationContract Operation(
        string kind,
        params DomainOperationParameterContract[] parameters)
    {
        var memberName = kind == DomainOperationKinds.Constructor
            ? ".ctor"
            : "From";
        return new DomainOperationContract(
            kind,
            "global::Example.Person",
            "global::Example.Person",
            memberName,
            parameters);
    }

    private static DomainFact<T> Fact<T>(
        T value,
        DomainFactProvenance provenance = DomainFactProvenance.Inferred,
        string sourceId = "test") =>
        new(value, provenance, sourceId, null);

    private static DomainTypeSpec TypeSpec(
        ImmutableArray<DomainRecipeSpec> recipes,
        ImmutableArray<DomainFact<DomainOperationContract>> operations,
        params DomainFact<DomainConstraintContract>[] constraints) =>
        new(
            "global::Example.Person",
            recipes,
            ImmutableArray<SubjectPropertySpec>.Empty,
            constraints.ToImmutableArray(),
            operations,
            ImmutableArray<DomainFact<DomainOperationOutcomeContract>>.Empty,
            ImmutableArray<DomainFact<DomainScenarioContract>>.Empty);

    private static FixtureGenerationSpec CreateFixtureConfiguration() =>
        new(
            "PersonFixture",
            "Example",
            "Valid",
            "global::Example.Person",
            "Person",
            "global::Example.PersonFixture.Baseline()",
            null,
            null,
            ImmutableArray<SubjectPropertySpec>.Empty,
            false,
            ImmutableArray<SubjectConstructorSpec>.Empty,
            false,
            false,
            null,
            ImmutableArray<DomainOperationContract>.Empty,
            null,
            true);
}
