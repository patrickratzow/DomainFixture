using System.Text;

namespace DomainFixture.SourceGenerator.Emission;

internal static class FluentValidationAdapterEmitter
{
    public static string Emit(
        string namespaceName,
        string className,
        string subjectTypeName,
        string validatorTypeName)
    {
        var source = new StringBuilder();
        source.Append("\nnamespace ").Append(namespaceName).Append("\n{\n")
            .Append("    internal sealed class ").Append(className)
            .Append(" : global::DomainFixture.Validation.IFixtureValidator<")
            .Append(subjectTypeName).Append(">\n    {\n")
            .Append("        private readonly ").Append(validatorTypeName)
            .Append(" _validator = new ").Append(validatorTypeName).Append("();\n\n")
            .Append("        public global::DomainFixture.Validation.ValidationReport Validate(")
            .Append(subjectTypeName).Append(" subject)\n        {\n")
            .Append("            var context = new global::FluentValidation.ValidationContext<")
            .Append(subjectTypeName).Append(">(subject);\n")
            .Append("            var result = _validator.Validate(context);\n")
            .Append("            var failures = new global::System.Collections.Generic.List<")
            .Append("global::DomainFixture.Validation.ValidationFailure>();\n")
            .Append("            foreach (var error in result.Errors)\n            {\n")
            .Append("                failures.Add(new global::DomainFixture.Validation.ValidationFailure(")
            .Append("error.PropertyName, error.ErrorCode, error.ErrorMessage));\n")
            .Append("            }\n\n")
            .Append("            return new global::DomainFixture.Validation.ValidationReport(failures);\n")
            .Append("        }\n    }\n}\n");

        return source.ToString();
    }
}
