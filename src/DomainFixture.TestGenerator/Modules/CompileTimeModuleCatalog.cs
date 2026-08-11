using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DomainFixture.Contracts;
using DomainFixture.Generation.Metadata;

namespace DomainFixture.TestGenerator.Modules;

/// <summary>
/// Validates and deterministically merges module metadata before any source is emitted.
/// </summary>
public sealed class CompileTimeModuleCatalog
{
    public ImmutableArray<CompileTimeModuleDescriptor> Modules { get; }
    public ImmutableArray<CompileTimeRecipeContribution> Recipes { get; }
    public ImmutableArray<CompileTimeValidationContribution> Validations { get; }
    public ImmutableArray<CompileTimeModuleIssue> Issues { get; }

    private CompileTimeModuleCatalog(
        ImmutableArray<CompileTimeModuleDescriptor> modules,
        ImmutableArray<CompileTimeRecipeContribution> recipes,
        ImmutableArray<CompileTimeValidationContribution> validations,
        ImmutableArray<CompileTimeModuleIssue> issues)
    {
        Modules = modules;
        Recipes = recipes;
        Validations = validations;
        Issues = issues;
    }

    public CompileTimeModuleCapabilityResolution ResolveCapability(
        string sourceTypeName,
        string capability)
    {
        var candidates = Modules
            .Where(module => module.SourceTypeName == sourceTypeName)
            .ToArray();
        if (candidates.Length == 0)
            return CompileTimeModuleCapabilityResolution.Unregistered(sourceTypeName, capability);
        if (candidates.Length > 1)
            return CompileTimeModuleCapabilityResolution.Ambiguous(sourceTypeName, capability);

        return candidates[0].Supports(capability)
            ? CompileTimeModuleCapabilityResolution.Supported(candidates[0], capability)
            : CompileTimeModuleCapabilityResolution.Missing(candidates[0], capability);
    }

    public static CompileTimeModuleCatalog Create(
        IEnumerable<CompileTimeModuleDescriptor> descriptors,
        IEnumerable<CompileTimeRecipeContribution> recipeContributions,
        IEnumerable<CompileTimeValidationContribution>? validationContributions = null)
    {
        var issues = ImmutableArray.CreateBuilder<CompileTimeModuleIssue>();
        var modules = ImmutableArray.CreateBuilder<CompileTimeModuleDescriptor>();
        var descriptorsById = (descriptors ?? Array.Empty<CompileTimeModuleDescriptor>())
            .GroupBy(descriptor => descriptor.ModuleId, StringComparer.Ordinal);

        foreach (var group in descriptorsById)
        {
            var candidates = group.ToArray();
            var first = candidates[0];
            if (!IsValid(first))
            {
                issues.Add(new CompileTimeModuleIssue(
                    first.SchemaVersion != DomainFixtureModuleManifestAttribute.CurrentSchemaVersion
                        ? CompileTimeModuleIssueKind.UnsupportedModuleSchema
                        : CompileTimeModuleIssueKind.InvalidModule,
                    first.ModuleId,
                    $"module '{first.ModuleId}' has an unsupported schema or missing identity, version, source type, or capabilities"));
                continue;
            }

            if (candidates.Skip(1).Any(candidate => !Equivalent(first, candidate)))
            {
                issues.Add(new CompileTimeModuleIssue(
                    CompileTimeModuleIssueKind.ConflictingModule,
                    first.ModuleId,
                    $"module id '{first.ModuleId}' is declared with conflicting versions, source types, or capabilities"));
                continue;
            }

            modules.Add(first);
        }

        var moduleById = modules.ToDictionary(module => module.ModuleId, StringComparer.Ordinal);
        var validRecipes = new List<CompileTimeRecipeContribution>();
        foreach (var recipe in recipeContributions ?? Array.Empty<CompileTimeRecipeContribution>())
        {
            if (recipe.SchemaVersion != DomainFixtureRecipeManifestAttribute.CurrentSchemaVersion)
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.UnsupportedRecipeSchema,
                    recipe,
                    $"recipe schema {recipe.SchemaVersion} is unsupported"));
                continue;
            }
            if (string.IsNullOrWhiteSpace(recipe.ModuleId) ||
                string.IsNullOrWhiteSpace(recipe.SourceTypeName) ||
                string.IsNullOrWhiteSpace(recipe.SubjectTypeName) ||
                string.IsNullOrWhiteSpace(recipe.ConfigurationNamespace) ||
                string.IsNullOrWhiteSpace(recipe.ConfigurationName) ||
                string.IsNullOrWhiteSpace(recipe.RecipeName))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.InvalidRecipe,
                    recipe,
                    "recipe metadata contains an empty required field"));
                continue;
            }
            if (!moduleById.TryGetValue(recipe.ModuleId, out var module))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.UnknownModule,
                    recipe,
                    $"recipe references unknown or invalid module '{recipe.ModuleId}'"));
                continue;
            }
            if (!module.Supports(DomainFixtureModuleCapabilities.Recipes))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.UnsupportedCapability,
                    recipe,
                    $"module '{recipe.ModuleId}' did not declare the recipes capability"));
                continue;
            }
            if (!string.Equals(module.SourceTypeName, recipe.SourceTypeName, StringComparison.Ordinal))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.SourceMismatch,
                    recipe,
                    $"recipe source '{recipe.SourceTypeName}' does not match module source '{module.SourceTypeName}'"));
                continue;
            }

            validRecipes.Add(recipe);
        }

        var recipes = ImmutableArray.CreateBuilder<CompileTimeRecipeContribution>();
        foreach (var group in validRecipes.GroupBy(
                     recipe => recipe.ConfigurationNamespace + "|" + recipe.ConfigurationName + "|" + recipe.RecipeName,
                     StringComparer.Ordinal))
        {
            var candidates = group.ToArray();
            var first = candidates[0];
            if (candidates.Skip(1).Any(candidate => !Equivalent(first, candidate)))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.ConflictingRecipe,
                    first,
                    $"generated recipe identity '{group.Key}' has conflicting module contributions"));
                continue;
            }

            recipes.Add(first);
        }

        var validValidations = new List<CompileTimeValidationContribution>();
        foreach (var validation in validationContributions ??
                 Array.Empty<CompileTimeValidationContribution>())
        {
            if (validation.SchemaVersion !=
                DomainFixtureValidationManifestAttribute.CurrentSchemaVersion)
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.UnsupportedValidationSchema,
                    validation,
                    $"validation schema {validation.SchemaVersion} is unsupported"));
                continue;
            }
            if (string.IsNullOrWhiteSpace(validation.ModuleId) ||
                string.IsNullOrWhiteSpace(validation.SourceTypeName) ||
                string.IsNullOrWhiteSpace(validation.SubjectTypeName) ||
                !IsGlobalTypeName(validation.AdapterTypeName))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.InvalidValidation,
                    validation,
                    "validation metadata contains an empty field or invalid global adapter type name"));
                continue;
            }
            if (!moduleById.TryGetValue(validation.ModuleId, out var module))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.UnknownModule,
                    validation,
                    $"validation binding references unknown or invalid module '{validation.ModuleId}'"));
                continue;
            }
            if (!module.Supports(DomainFixtureModuleCapabilities.Validators))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.UnsupportedCapability,
                    validation,
                    $"module '{validation.ModuleId}' did not declare the validators capability"));
                continue;
            }
            if (module.SourceTypeName != validation.SourceTypeName)
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.SourceMismatch,
                    validation,
                    $"validation source '{validation.SourceTypeName}' does not match module source '{module.SourceTypeName}'"));
                continue;
            }

            validValidations.Add(validation);
        }

        var validations = ImmutableArray.CreateBuilder<CompileTimeValidationContribution>();
        foreach (var group in validValidations.GroupBy(
                     validation => validation.SubjectTypeName,
                     StringComparer.Ordinal))
        {
            var candidates = group.ToArray();
            var first = candidates[0];
            if (candidates.Skip(1).Any(candidate => !Equivalent(first, candidate)))
            {
                issues.Add(Issue(
                    CompileTimeModuleIssueKind.ConflictingValidation,
                    first,
                    $"subject '{group.Key}' has conflicting module validation adapters"));
                continue;
            }

            validations.Add(first);
        }

        return new CompileTimeModuleCatalog(
            modules.OrderBy(module => module.ModuleId, StringComparer.Ordinal).ToImmutableArray(),
            recipes.ToImmutable(),
            validations.ToImmutable(),
            issues.ToImmutable());
    }

    private static bool IsValid(CompileTimeModuleDescriptor descriptor) =>
        descriptor.SchemaVersion == DomainFixtureModuleManifestAttribute.CurrentSchemaVersion &&
        !string.IsNullOrWhiteSpace(descriptor.ModuleId) &&
        !string.IsNullOrWhiteSpace(descriptor.ModuleVersion) &&
        !string.IsNullOrWhiteSpace(descriptor.SourceTypeName) &&
        !descriptor.Capabilities.IsEmpty;

    private static bool Equivalent(
        CompileTimeModuleDescriptor first,
        CompileTimeModuleDescriptor second) =>
        first.SchemaVersion == second.SchemaVersion &&
        first.ModuleId == second.ModuleId &&
        first.ModuleVersion == second.ModuleVersion &&
        first.SourceTypeName == second.SourceTypeName &&
        first.Capabilities.SequenceEqual(second.Capabilities, StringComparer.Ordinal);

    private static bool Equivalent(
        CompileTimeRecipeContribution first,
        CompileTimeRecipeContribution second) =>
        first.SchemaVersion == second.SchemaVersion &&
        first.ModuleId == second.ModuleId &&
        first.SourceTypeName == second.SourceTypeName &&
        first.SubjectTypeName == second.SubjectTypeName &&
        first.ConfigurationNamespace == second.ConfigurationNamespace &&
        first.ConfigurationName == second.ConfigurationName &&
        first.RecipeName == second.RecipeName;

    private static bool Equivalent(
        CompileTimeValidationContribution first,
        CompileTimeValidationContribution second) =>
        first.SchemaVersion == second.SchemaVersion &&
        first.ModuleId == second.ModuleId &&
        first.SourceTypeName == second.SourceTypeName &&
        first.SubjectTypeName == second.SubjectTypeName &&
        first.AdapterTypeName == second.AdapterTypeName;

    private static bool IsGlobalTypeName(string value)
    {
        const string prefix = "global::";
        if (string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        return value.Substring(prefix.Length)
            .Split('.')
            .All(part => part.Length > 0 &&
                         (char.IsLetter(part[0]) || part[0] is '_' or '@') &&
                         part.Skip(1).All(character =>
                             char.IsLetterOrDigit(character) || character == '_'));
    }

    private static CompileTimeModuleIssue Issue(
        CompileTimeModuleIssueKind kind,
        CompileTimeRecipeContribution recipe,
        string message) =>
        new(kind, recipe.ModuleId, message);

    private static CompileTimeModuleIssue Issue(
        CompileTimeModuleIssueKind kind,
        CompileTimeValidationContribution validation,
        string message) =>
        new(kind, validation.ModuleId, message);
}
