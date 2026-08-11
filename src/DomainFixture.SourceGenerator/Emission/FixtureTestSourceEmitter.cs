using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DomainFixture.SourceGenerator.Diagnostics;
using DomainFixture.SourceGenerator.Extraction;
using DomainFixture.SourceGenerator.Models;
using DomainFixture.TestGenerator.Boundaries;
using DomainFixture.TestGenerator.Framework.Emitters;
using DomainFixture.TestGenerator.Generation;
using DomainFixture.TestGenerator.Model.Properties;
using DomainFixture.TestGenerator.Model.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace DomainFixture.SourceGenerator.Emission;

internal static class FixtureTestSourceEmitter
{
    public static void Emit(
        SourceProductionContext context,
        ConfigurationParseResult configurationResult,
        GenerationProfileParseResult profileResult,
        ImmutableArray<ValidationRuleSpec> rules)
    {
        if (configurationResult.Diagnostics.Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error) ||
            profileResult.Diagnostics.Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        foreach (var configuration in configurationResult.Configurations)
        {
            var propertyMutations = profileResult.Profile.PropertyMutations
                .Where(mutation => mutation.SubjectTypeKey == configuration.SubjectTypeName)
                .ToDictionary(mutation => mutation.PropertyName);
            var matchingRules = rules
                .Where(rule => rule.ValidationRulesTypeKey == configuration.ValidationRulesTypeKey)
                .Concat(ConventionRuleProvider.Create(configuration, profileResult.Profile))
                .GroupBy(rule => new { rule.PropertyName, rule.Kind })
                .Select(group => group.First())
                .ToArray();
            if (matchingRules.Length == 0)
            {
                context.ReportDiagnostic(GeneratorDiagnostics.MissingSupportedRules(
                    configuration.Location,
                    configuration.ValidationRulesTypeKey));
                continue;
            }

            var inaccessibleRule = matchingRules.FirstOrDefault(rule =>
                !rule.PropertyCanBeAssigned &&
                !propertyMutations.ContainsKey(rule.PropertyName));
            if (inaccessibleRule is not null)
            {
                context.ReportDiagnostic(GeneratorDiagnostics.PropertySetterInaccessible(
                    configuration.Location,
                    inaccessibleRule.PropertyName));
                continue;
            }

            var cases = matchingRules
                .SelectMany(GenerateCases)
                .Select(validationCase => ApplyReconstruction(
                    validationCase,
                    propertyMutations))
                .ToArray();
            var descriptor = new ValidationTestSuiteDescriptor(
                configuration.NamespaceName,
                $"{configuration.SubjectTypeShortName}{configuration.RecipeName}GeneratedTests",
                configuration.RecipeName,
                SyntaxFactory.ParseTypeName(configuration.SubjectTypeName),
                SyntaxFactory.ParseExpression(configuration.BaselineFactoryExpression),
                SyntaxFactory.ParseExpression(configuration.ValidatorFactoryExpression),
                cases);
            var suite = new ValidationTestSuiteBuilder().Build(descriptor);
            var source = new NUnitTestEmitter().EmitSource(suite);
            var hintName = $"{configuration.ConfigurationName}.{configuration.RecipeName}.g.cs";

            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static GeneratedValidationCase ApplyReconstruction(
        GeneratedValidationCase validationCase,
        IReadOnlyDictionary<string, PropertyMutationSpec> propertyMutations)
    {
        if (!propertyMutations.TryGetValue(
                validationCase.Mutation.Property.Name,
                out var mutation))
        {
            return validationCase;
        }

        return new GeneratedValidationCase(
            validationCase.Name,
            new PropertyMutationDescriptor(
                validationCase.Mutation.Property,
                validationCase.Mutation.Value,
                SyntaxFactory.ParseExpression(mutation.ReconstructionExpression)),
            validationCase.ExpectedOutcome,
            validationCase.ErrorCode);
    }

    private static IEnumerable<GeneratedValidationCase> GenerateCases(ValidationRuleSpec rule)
    {
        var property = new PropertyDescriptor(rule.PropertyName);

        switch (rule.Kind)
        {
            case ValidationRuleKind.StringLength when rule.Minimum is not null && rule.Maximum is not null:
                return new StringLengthBoundaryCaseGenerator().Generate(
                    new StringLengthConstraintDescriptor(
                        property,
                        rule.Minimum.Value,
                        rule.Maximum.Value,
                        rule.ErrorCode));

            case ValidationRuleKind.StringMaximumLength when rule.Maximum is not null:
                return new StringMaximumLengthBoundaryCaseGenerator().Generate(
                    new StringMaximumLengthConstraintDescriptor(
                        property,
                        rule.Maximum.Value,
                        rule.ErrorCode));

            case ValidationRuleKind.NotEmpty:
                return new[]
                {
                    new StringPresenceBoundaryCaseGenerator().Generate(
                        new StringPresenceConstraintDescriptor(
                            property,
                            StringPresenceConstraintKind.NotEmpty,
                            rule.ErrorCode))
                };

            case ValidationRuleKind.NotNull:
                return new[]
                {
                    new StringPresenceBoundaryCaseGenerator().Generate(
                        new StringPresenceConstraintDescriptor(
                            property,
                            StringPresenceConstraintKind.NotNull,
                            rule.ErrorCode))
                };

            default:
                return Enumerable.Empty<GeneratedValidationCase>();
        }
    }
}
