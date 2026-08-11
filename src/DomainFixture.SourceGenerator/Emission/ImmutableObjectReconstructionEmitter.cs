using System.Collections.Generic;
using System.Linq;
using System.Text;
using DomainFixture.SourceGenerator.Models;

namespace DomainFixture.SourceGenerator.Emission;

internal static class ImmutableObjectReconstructionEmitter
{
    public static string Emit(
        FixtureGenerationSpec configuration,
        string className,
        IReadOnlyCollection<SubjectPropertySpec> targetProperties)
    {
        var stateProperties = configuration.SubjectProperties
            .Where(property => property.HasSetter)
            .ToArray();
        var source = new StringBuilder();
        source.Append("\nnamespace ").Append(configuration.NamespaceName).Append("\n{\n")
            .Append("    internal sealed class ").Append(className).Append(" : ")
            .Append(configuration.SubjectTypeName).Append("\n    {\n")
            .Append("        private ").Append(className).Append("()\n        {\n        }\n");

        foreach (var target in targetProperties)
        {
            source.Append("\n        internal static ")
                .Append(configuration.SubjectTypeName)
                .Append(" With").Append(target.Name).Append('(')
                .Append(configuration.SubjectTypeName).Append(" source, ")
                .Append(target.TypeName).Append(" value)\n        {\n")
                .Append("            return new ").Append(className).Append("\n            {\n");

            for (var index = 0; index < stateProperties.Length; index++)
            {
                var property = stateProperties[index];
                source.Append("                ").Append(property.Name).Append(" = ")
                    .Append(property.Name == target.Name
                        ? "value"
                        : $"source.{property.Name}");
                source.Append(index == stateProperties.Length - 1 ? "\n" : ",\n");
            }

            source.Append("            };\n        }\n");
        }

        source.Append("    }\n}\n");
        return source.ToString();
    }
}
