using System.Collections.Generic;
using System.Linq;
using System.Text;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Emission;

internal static class PropertyMutationReconstructionEmitter
{
    public static string Emit(
        FixtureGenerationSpec configuration,
        string className,
        IReadOnlyCollection<PropertyMutationResolution> resolutions)
    {
        var automatic = resolutions
            .Where(resolution => resolution.Strategy is
                PropertyMutationStrategyKind.RecordWith or
                PropertyMutationStrategyKind.Constructor or
                PropertyMutationStrategyKind.DerivedType)
            .ToArray();
        if (automatic.Length == 0)
            return string.Empty;

        return EmitAutomatic(configuration, className, automatic);
    }

    private static string EmitAutomatic(
        FixtureGenerationSpec configuration,
        string className,
        IEnumerable<PropertyMutationResolution> resolutions)
    {
        var resolved = resolutions.ToArray();
        var stateProperties = configuration.SubjectProperties
            .Where(property => property.HasSetter)
            .ToArray();
        var source = new StringBuilder();
        var usesDerivedType = resolved.Any(resolution =>
            resolution.Strategy == PropertyMutationStrategyKind.DerivedType);
        source.Append("\nnamespace ").Append(configuration.NamespaceName).Append("\n{\n");
        if (usesDerivedType)
        {
            source.Append("    internal sealed class ").Append(className).Append(" : ")
                .Append(configuration.SubjectTypeName).Append("\n    {\n")
                .Append("        private ").Append(className).Append("()\n        {\n        }\n");
        }
        else
        {
            source.Append("    internal static class ").Append(className).Append("\n    {\n");
        }

        foreach (var resolution in resolved)
        {
            var target = resolution.Property;
            AppendMethodSignature(source, configuration, target);
            switch (resolution.Strategy)
            {
                case PropertyMutationStrategyKind.RecordWith:
                    source.Append("            return source with { ")
                        .Append(target.Name)
                        .Append(" = value };\n");
                    break;

                case PropertyMutationStrategyKind.Constructor:
                    source.Append("            return new ")
                        .Append(configuration.SubjectTypeName)
                        .Append('(');
                    var parameters = resolution.Constructor!.Parameters;
                    for (var index = 0; index < parameters.Length; index++)
                    {
                        if (index > 0)
                            source.Append(", ");

                        var propertyName = parameters[index].PropertyName;
                        source.Append(propertyName == target.Name
                            ? "value"
                            : $"source.{propertyName}");
                    }

                    source.Append(");\n");
                    break;

                case PropertyMutationStrategyKind.DerivedType:
                    source.Append("            return new ").Append(className)
                        .Append("\n            {\n");
                    for (var index = 0; index < stateProperties.Length; index++)
                    {
                        var property = stateProperties[index];
                        source.Append("                ").Append(property.Name).Append(" = ")
                            .Append(property.Name == target.Name
                                ? "value"
                                : $"source.{property.Name}")
                            .Append(index == stateProperties.Length - 1 ? "\n" : ",\n");
                    }

                    source.Append("            };\n");
                    break;
            }

            source.Append("        }\n");
        }

        return EndClass(source);
    }

    private static void AppendMethodSignature(
        StringBuilder source,
        FixtureGenerationSpec configuration,
        SubjectPropertySpec property)
    {
        source.Append("\n        internal static ")
            .Append(configuration.SubjectTypeName)
            .Append(" With").Append(property.Name).Append('(')
            .Append(configuration.SubjectTypeName).Append(" source, ")
            .Append(property.TypeName).Append(" value)\n        {\n");
    }

    private static string EndClass(StringBuilder source)
    {
        source.Append("    }\n}\n");
        return source.ToString();
    }
}
