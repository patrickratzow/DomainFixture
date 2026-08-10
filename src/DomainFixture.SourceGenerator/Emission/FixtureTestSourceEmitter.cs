using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DomainFixture.SourceGenerator.Diagnostics;
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
        ImmutableArray<ValidationRuleSpec> rules)
    {
        if (configurationResult.Diagnostics.Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        foreach (var configuration in configurationResult.Configurations)
        {
            var matchingRules = rules
                .Where(rule => rule.ValidationRulesTypeKey == configuration.ValidationRulesTypeKey)
                .ToArray();
            if (matchingRules.Length == 0)
            {
                context.ReportDiagnostic(GeneratorDiagnostics.MissingSupportedRules(
                    configuration.Location,
                    configuration.ValidationRulesTypeKey));
                continue;
            }

            var boundaryGenerator = new StringLengthBoundaryCaseGenerator();
            var cases = matchingRules
                .Select(rule => new StringLengthConstraintDescriptor(
                    new PropertyDescriptor(rule.PropertyName),
                    rule.Minimum,
                    rule.Maximum,
                    rule.ErrorCode))
                .SelectMany(boundaryGenerator.Generate)
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
}
