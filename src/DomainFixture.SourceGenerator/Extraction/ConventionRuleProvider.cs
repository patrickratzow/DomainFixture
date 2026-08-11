using System;
using System.Collections.Generic;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Extraction;

internal static class ConventionConstraintProvider
{
    private static readonly string[] NonEmptyNameSuffixes =
    {
        "Email",
        "EmailAddress",
        "Name",
        "Username",
        "Description"
    };

    public static IEnumerable<DiscoveredDomainConstraint> Create(
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
                    DomainConstraintKinds.TextNotNull);
            }

            if (profile.UsePropertyNames && HasNonEmptySemantic(property.Name))
            {
                yield return CreateRule(
                    configuration,
                    property,
                    validationRulesTypeKey,
                    DomainConstraintKinds.TextNotEmpty);
            }
        }
    }

    private static bool HasNonEmptySemantic(string propertyName) =>
        NonEmptyNameSuffixes.Any(suffix =>
            propertyName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    private static DiscoveredDomainConstraint CreateRule(
        FixtureGenerationSpec configuration,
        SubjectPropertySpec property,
        string validationRulesTypeKey,
        string kindId) =>
        new(
            validationRulesTypeKey,
            configuration.SubjectTypeName,
            property.Name,
            kindId,
            minimum: null,
            maximum: null,
            errorCode: null,
            propertyCanBeAssigned: property.CanBeAssigned,
            location: configuration.Location);
}
