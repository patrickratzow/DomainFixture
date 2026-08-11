using System;
using System.Collections.Generic;
using System.Linq;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class ConventionRuleProvider
{
    private static readonly string[] NonEmptyNameSuffixes =
    {
        "Email",
        "EmailAddress",
        "Name",
        "Username",
        "Description"
    };

    public static IEnumerable<ValidationRuleSpec> Create(
        FixtureGenerationSpec configuration,
        GenerationProfileSpec profile,
        string validationRulesTypeKey)
    {
        foreach (var property in configuration.SubjectProperties.Where(property => property.IsString))
        {
            if (profile.UseNullability && property.IsNonNullable)
            {
                yield return CreateRule(
                    configuration,
                    property,
                    validationRulesTypeKey,
                    ValidationRuleKind.NotNull);
            }

            if (profile.UsePropertyNames && HasNonEmptySemantic(property.Name))
            {
                yield return CreateRule(
                    configuration,
                    property,
                    validationRulesTypeKey,
                    ValidationRuleKind.NotEmpty);
            }
        }
    }

    private static bool HasNonEmptySemantic(string propertyName) =>
        NonEmptyNameSuffixes.Any(suffix =>
            propertyName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    private static ValidationRuleSpec CreateRule(
        FixtureGenerationSpec configuration,
        SubjectPropertySpec property,
        string validationRulesTypeKey,
        ValidationRuleKind kind) =>
        new(
            validationRulesTypeKey,
            configuration.SubjectTypeName,
            property.Name,
            kind,
            minimum: null,
            maximum: null,
            errorCode: null,
            propertyCanBeAssigned: property.CanBeAssigned,
            location: configuration.Location);
}
