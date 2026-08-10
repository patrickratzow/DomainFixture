using Microsoft.CodeAnalysis;

namespace DomainFixture.SourceGenerator.Models;

internal sealed class RuleExtractionResult
{
    public ValidationRuleSpec? Rule { get; }
    public Diagnostic? Diagnostic { get; }

    private RuleExtractionResult(ValidationRuleSpec? rule, Diagnostic? diagnostic)
    {
        Rule = rule;
        Diagnostic = diagnostic;
    }

    public static RuleExtractionResult Success(ValidationRuleSpec rule) => new(rule, null);
    public static RuleExtractionResult Failure(Diagnostic diagnostic) => new(null, diagnostic);
}
