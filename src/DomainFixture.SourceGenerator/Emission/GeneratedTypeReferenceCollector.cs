using System.Collections.Generic;
using System.Text.RegularExpressions;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Emission;

internal static class GeneratedTypeReferenceCollector
{
    public static IEnumerable<string> Collect(
        FixtureGenerationSpec configuration,
        IEnumerable<DomainOperationContract>? operations = null,
        IEnumerable<DomainOperationOutcomeContract>? outcomes = null,
        string? resolvedValidationRulesTypeKey = null)
    {
        yield return configuration.SubjectTypeName;
        yield return $"global::{configuration.NamespaceName}.{configuration.ConfigurationName}";
        yield return $"global::{configuration.NamespaceName}.{configuration.ConfigurationName}Factory";
        foreach (Match match in Regex.Matches(
                     configuration.BaselineFactoryExpression,
                     @"global::(?:@?[A-Za-z_][A-Za-z0-9_]*\.)+@?[A-Za-z_][A-Za-z0-9_]*Factory\b"))
            yield return match.Value;
        yield return
            $"global::{configuration.NamespaceName}.{configuration.SubjectTypeShortName}{configuration.RecipeName}FluentValidationAdapter";
        yield return
            $"global::{configuration.NamespaceName}.{configuration.SubjectTypeShortName}{configuration.RecipeName}ImmutableReconstruction";
        var validationRulesTypeKey =
            resolvedValidationRulesTypeKey ?? configuration.ValidationRulesTypeKey;
        if (validationRulesTypeKey is not null)
            yield return validationRulesTypeKey;
        yield return "global::DomainFixture.Validation.ValidationReport";
        yield return "global::DomainFixture.Validation.ValidationFailure";
        yield return $"global::DomainFixture.Validation.IFixtureValidator<{configuration.SubjectTypeName}>";
        yield return $"global::FluentValidation.ValidationContext<{configuration.SubjectTypeName}>";

        foreach (var property in configuration.SubjectProperties)
            yield return property.TypeName;
        foreach (var operation in operations ?? configuration.ConstructionOperations)
        {
            yield return operation.DeclaringTypeName;
            yield return operation.SubjectTypeName;
            yield return operation.ReturnTypeName;
            foreach (var parameter in operation.Parameters)
                yield return parameter.TypeName;
        }

        foreach (var transition in configuration.Transitions)
        {
            yield return transition.Operation.DeclaringTypeName;
            yield return transition.Operation.ReturnTypeName;
            if (transition.RejectionExceptionTypeName is not null)
                yield return transition.RejectionExceptionTypeName;
            if (transition.ResultTypeName is not null)
                yield return transition.ResultTypeName;
            foreach (var argument in transition.Arguments)
                yield return argument.TypeName;
        }

        if (outcomes is null)
            yield break;
        foreach (var outcome in outcomes)
        {
            if (outcome.KindId == DomainOperationOutcomeKinds.ThrowsException &&
                outcome.Parameters.Count == 1)
                yield return outcome.Parameters[0];
        }
    }
}
